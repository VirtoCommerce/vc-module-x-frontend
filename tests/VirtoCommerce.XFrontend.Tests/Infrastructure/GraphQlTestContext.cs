using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using GraphQL;
using GraphQL.Introspection;
using GraphQL.Types;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.Xapi.Data.Services;
using VirtoCommerce.XFrontend.Core;
using VirtoCommerce.XFrontend.Core.Layouts.Commands;
using VirtoCommerce.XFrontend.Core.Layouts.Services;
using VirtoCommerce.XFrontend.Core.Statistics.Services;
using VirtoCommerce.XFrontend.Data;
using VirtoCommerce.XFrontend.Data.Layouts.Services;
using VirtoCommerce.XFrontend.Data.Layouts.Validation;
using VirtoCommerce.XFrontend.Data.Queries;
using VirtoCommerce.XFrontend.Data.Statistics.Services;
using Xunit;
using CustomerModuleConstants = VirtoCommerce.CustomerModule.Core.ModuleConstants;

namespace VirtoCommerce.XFrontend.Tests.Infrastructure;

/// <summary>
/// Runs GraphQL documents through the module's real schema registration (graph types, MediatR handlers, schema
/// builders), the real currency resolver, layout service and validator, and the real Xapi account-state check
/// (<see cref="UserManagerCore"/> over a fake user store). The order statistics service is a recording stand-in: the
/// GraphQL tests assert what scope reaches it, while the service tests cover the aggregation itself.
/// </summary>
public sealed class GraphQlTestContext : IDisposable
{
    public const string StoreId = "B2B-store";

    // Its default currency (GBP) is not one the platform has configured.
    public const string UnconfiguredCurrencyStoreId = "PoundStore";

    private readonly ServiceProvider _provider;
    private readonly Dictionary<string, ApplicationUser> _users = [];
    private readonly HashSet<string> _lockedUserIds = [];

    public GraphQlTestContext()
    {
        // The store's default currency (EUR) differs from the platform's primary one (USD), so tests can tell them apart.
        var storeService = new Mock<IStoreService>();
        storeService
            .Setup(x => x.GetAsync(It.Is<IList<string>>(ids => ids.Contains(StoreId)), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([new Store { Id = StoreId, DefaultCurrency = "EUR" }]);
        storeService
            .Setup(x => x.GetAsync(It.Is<IList<string>>(ids => ids.Contains(UnconfiguredCurrencyStoreId)), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([new Store { Id = UnconfiguredCurrencyStoreId, DefaultCurrency = "GBP" }]);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();
        services.AddSingleton<ISchemaFilter, DefaultSchemaFilter>();
        services.AddSingleton<ICurrencyService, TestCurrencyService>();
        services.AddSingleton(storeService.Object);
        services.AddTransient<IStatisticsCurrencyResolver, StatisticsCurrencyResolver>();
        services.AddSingleton<IOrderStatisticsService>(Statistics);
        services.AddSingleton<ICustomerPreferenceService>(Preferences);
        services.AddSingleton<ICustomerPreferenceSearchService>(Preferences);
        services.AddTransient<ILayoutService, LayoutService>();
        services.AddTransient<AbstractValidator<SaveLayoutCommand>, SaveLayoutCommandValidator>();
        services.AddSingleton<IUserManagerCore>(new UserManagerCore(CreateUserManager, requestScopedCacheAccessor: null));

        services.AddGraphQL(builder =>
        {
            // The module's own registration: Core graph types, MediatR handlers and every schema builder of Data.
            builder.AddSchema(services, typeof(CoreAssemblyMarker), typeof(DataAssemblyMarker));
            builder.AddGraphTypes(typeof(MoneyType).Assembly);
            builder.AddSystemTextJson();
            builder.AddDataLoader();
        });

        // pageContext returns the profile and white-labeling graphs, which are not what these tests are about.
        services.Remove(services.Single(x => x.ServiceType == typeof(ISchemaBuilder) && x.ImplementationType == typeof(PageContextQueryBuilder)));

        services.AddSingleton<SchemaFactory>();

        _provider = services.BuildServiceProvider();
    }

    public RecordingOrderStatisticsService Statistics { get; } = new();

    public InMemoryCustomerPreferenceService Preferences { get; } = new();

    public ISchema Schema => _provider.GetRequiredService<SchemaFactory>();

    /// <summary>
    /// Creates an account for the account-state check: usable by default, or locked, or with an expired password.
    /// An administrator's token also carries the platform Administrator role.
    /// </summary>
    public void AddUser(string userId, bool locked = false, bool passwordExpired = false, bool isAdministrator = false)
    {
        _users[userId] = new ApplicationUser
        {
            Id = userId,
            UserName = userId,
            PasswordExpired = passwordExpired,
            IsAdministrator = isAdministrator,
        };

        if (locked)
        {
            _lockedUserIds.Add(userId);
        }
    }

    /// <summary>Executes as a signed-in user; <paramref name="organizationId"/> becomes the token's organization claim.</summary>
    public Task<string> ExecuteAsync(string query, string userId, string organizationId = null)
    {
        var identity = new ClaimsIdentity(authenticationType: "Test");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));

        if (organizationId != null)
        {
            identity.AddClaim(new Claim(CustomerModuleConstants.Security.Claims.OrganizationId, organizationId));
        }

        if (_users.TryGetValue(userId, out var user) && user.IsAdministrator)
        {
            identity.AddClaim(new Claim(identity.RoleClaimType, PlatformConstants.Security.SystemRoles.Administrator));
        }

        return ExecuteAsync(query, new ClaimsPrincipal(identity));
    }

    public Task<string> ExecuteAnonymousAsync(string query)
    {
        return ExecuteAsync(query, new ClaimsPrincipal(new ClaimsIdentity()));
    }

    /// <summary>The <c>data.{field}</c> node, after asserting the response carries no errors.</summary>
    public static JsonElement Data(string json, string field)
    {
        using var document = JsonDocument.Parse(json);
        document.RootElement.TryGetProperty("errors", out _).Should().BeFalse("the response should carry no errors: {0}", json);

        return document.RootElement.GetProperty("data").GetProperty(field).Clone();
    }

    /// <summary>The response's errors as (message, code) pairs; empty when there are none.</summary>
    public static IList<(string Message, string Code)> Errors(string json)
    {
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("errors", out var errors))
        {
            return [];
        }

        return errors.EnumerateArray()
            .Select(x => (
                x.GetProperty("message").GetString(),
                x.TryGetProperty("extensions", out var extensions) && extensions.TryGetProperty("code", out var code) ? code.GetString() : null))
            .ToList();
    }

    public void Dispose()
    {
        _provider.Dispose();
    }

    private async Task<string> ExecuteAsync(string query, ClaimsPrincipal principal)
    {
        using var scope = _provider.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<IDocumentExecuter>().ExecuteAsync(options =>
        {
            options.Schema = Schema;
            options.Query = query;
            options.RequestServices = scope.ServiceProvider;
            options.UserContext = new GraphQLUserContext(principal);
            options.CancellationToken = TestContext.Current.CancellationToken;
        });

        return scope.ServiceProvider.GetRequiredService<IGraphQLTextSerializer>().Serialize(result);
    }

    private UserManager<ApplicationUser> CreateUserManager()
    {
        var userManager = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);

        userManager
            .Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string userId) => userId != null && _users.TryGetValue(userId, out var user) ? user : null);

        userManager
            .Setup(x => x.IsLockedOutAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync((ApplicationUser user) => _lockedUserIds.Contains(user.Id));

        return userManager.Object;
    }
}

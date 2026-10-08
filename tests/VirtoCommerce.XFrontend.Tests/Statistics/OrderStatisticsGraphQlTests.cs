using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.XFrontend.Core.Statistics.Models;
using VirtoCommerce.XFrontend.Tests.Infrastructure;
using Xunit;

namespace VirtoCommerce.XFrontend.Tests.Statistics;

/// <summary>
/// The <c>orderStatistics</c> query through the module's real schema, handler and currency resolver. The scope must
/// come from the token alone (the buyer's own orders), and every refused caller must leave the statistics service
/// untouched.
/// </summary>
[Trait("Category", "Unit")]
public sealed class OrderStatisticsGraphQlTests : IDisposable
{
    private const string User = "user-1";

    private const string YearToDate = "from: \"2026-01-01T00:00:00Z\", to: \"2026-07-11T00:00:00Z\"";
    private const string LastYear = "from: \"2025-01-01T00:00:00Z\", to: \"2026-01-01T00:00:00Z\"";
    private const string LastMonth = "from: \"2026-06-01T00:00:00Z\", to: \"2026-06-30T00:00:00Z\"";

    private static readonly DateTime _yearToDateFrom = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _lastYearFrom = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly GraphQlTestContext _context = new();

    public OrderStatisticsGraphQlTests()
    {
        _context.AddUser(User);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Anonymous_IsDenied()
    {
        var json = await _context.ExecuteAnonymousAsync(Query("storeId: \"B2B-store\""));

        GraphQlTestContext.Errors(json).Should().ContainSingle().Which.Code.Should().Be(Constants.UnauthorizedCode);
        _context.Statistics.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task LockedAccount_IsDenied()
    {
        // The token is still valid after the account is locked; only the account-state check can refuse it.
        _context.AddUser("locked-user", locked: true);

        var json = await _context.ExecuteAsync(Query("storeId: \"B2B-store\""), "locked-user");

        GraphQlTestContext.Errors(json).Should().ContainSingle().Which.Code.Should().Be(Constants.UserLockedCode);
        _context.Statistics.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task PasswordExpiredAccount_IsDenied()
    {
        _context.AddUser("expired-user", passwordExpired: true);

        var json = await _context.ExecuteAsync(Query("storeId: \"B2B-store\""), "expired-user");

        GraphQlTestContext.Errors(json).Should().ContainSingle().Which.Code.Should().Be(Constants.PasswordExpiredCode);
        _context.Statistics.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task DeletedAccount_IsDenied()
    {
        var json = await _context.ExecuteAsync(Query("storeId: \"B2B-store\""), "deleted-user");

        GraphQlTestContext.Errors(json).Should().ContainSingle().Which.Code.Should().Be(Constants.UnauthorizedCode);
        _context.Statistics.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Scope_IsStampedFromTheToken()
    {
        var json = await _context.ExecuteAsync(Query("storeId: \"B2B-store\", currencyCode: \"USD\""), User, organizationId: "org-1");

        GraphQlTestContext.Data(json, "orderStatistics").GetProperty("currencyCode").GetString().Should().Be("USD");

        var criteria = _context.Statistics.Calls.Should().ContainSingle().Subject;
        criteria.CustomerId.Should().Be(User);
        criteria.OrganizationIds.Should().Equal("org-1");
        criteria.StoreId.Should().Be("B2B-store");
        criteria.CurrencyCode.Should().Be("USD");
        criteria.FromDate.Should().Be(_yearToDateFrom);
    }

    [Fact]
    public async Task Administrator_GetsTheirOwnFigures()
    {
        // No store-wide or organization-wide branch for administrators: the scope is the caller's own orders, like anyone's.
        _context.AddUser("admin-user", isAdministrator: true);

        var json = await _context.ExecuteAsync(Query("storeId: \"B2B-store\""), "admin-user", organizationId: "org-9");

        GraphQlTestContext.Data(json, "orderStatistics");
        var criteria = _context.Statistics.Calls.Should().ContainSingle().Subject;
        criteria.CustomerId.Should().Be("admin-user");
        criteria.OrganizationIds.Should().Equal("org-9");
    }

    [Fact]
    public async Task UserWithoutOrganization_IsNotFilteredByOrganization()
    {
        // A B2C buyer has no organization claim: their own orders count wherever they were placed.
        var json = await _context.ExecuteAsync(Query("storeId: \"B2B-store\""), User);

        GraphQlTestContext.Data(json, "orderStatistics");
        var criteria = _context.Statistics.Calls.Should().ContainSingle().Subject;
        criteria.CustomerId.Should().Be(User);
        criteria.OrganizationIds.Should().BeNull();
    }

    [Theory]
    [InlineData("storeId: \"B2B-store\"", "EUR")] // the store's default currency
    [InlineData("storeId: \"B2B-store\", currencyCode: \"usd\"", "USD")] // the requested one, as configured
    [InlineData("cultureName: \"en-US\"", "USD")] // no store: the platform's primary currency
    public async Task Currency_DefaultsToTheStoreThenThePrimaryCurrency(string arguments, string expected)
    {
        var json = await _context.ExecuteAsync(Query(arguments), User);

        GraphQlTestContext.Data(json, "orderStatistics").GetProperty("currencyCode").GetString().Should().Be(expected);
        _context.Statistics.Calls.Should().ContainSingle().Which.CurrencyCode.Should().Be(expected);
    }

    [Fact]
    public async Task EmptyAndOmittedStoreId_AreOneScope()
    {
        // Both mean all stores: one DataLoader key and one criteria, so one aggregation (and one cache entry).
        var json = await _context.ExecuteAsync(
            $$"""
              query {
                empty: orderStatistics(storeId: "") { ytd: period({{YearToDate}}) { count } }
                omitted: orderStatistics { ytd: period({{YearToDate}}) { count } }
              }
              """,
            User);

        GraphQlTestContext.Data(json, "empty");
        _context.Statistics.Calls.Should().ContainSingle().Which.StoreId.Should().BeNull();
    }

    [Fact]
    public async Task UnknownCurrency_IsAnExecutionError()
    {
        var json = await _context.ExecuteAsync(Query("storeId: \"B2B-store\", currencyCode: \"XYZ\""), User);

        // A clean message, not the generic "Error trying to resolve field" of an unhandled exception.
        GraphQlTestContext.Errors(json).Should().ContainSingle().Which.Message.Should().Be("Currency 'XYZ' is not configured.");
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("data").GetProperty("orderStatistics").ValueKind.Should().Be(JsonValueKind.Null);
        _context.Statistics.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task UnconfiguredStoreDefaultCurrency_IsAnExecutionErrorNamingTheStore()
    {
        var json = await _context.ExecuteAsync(Query($"storeId: \"{GraphQlTestContext.UnconfiguredCurrencyStoreId}\""), User);

        GraphQlTestContext.Errors(json).Should().ContainSingle().Which.Message
            .Should().Be("No currency was requested and no configured currency could be resolved for store 'PoundStore'.");
        _context.Statistics.Calls.Should().BeEmpty();
    }

    [Fact]
    public void Schema_HasNoIdentityArgument()
    {
        // Whose orders are counted is decided by the token only; an argument naming a user would be a way around it.
        _context.Schema.Initialize();

        var field = _context.Schema.Query.GetField("orderStatistics");

        field.Arguments.Select(x => x.Name).Should().BeEquivalentTo("storeId", "currencyCode", "cultureName");
    }

    [Fact]
    public async Task EachDistinctRange_IsAggregatedOncePerRequest()
    {
        // Four uses of two ranges (two periods, the two sides of a comparison) must cost two aggregations.
        var json = await _context.ExecuteAsync(
            $$"""
              query {
                orderStatistics(storeId: "B2B-store") {
                  ytd: period({{YearToDate}}) { count }
                  lastYear: period({{LastYear}}) { count }
                  ytdVsLastYear: comparison(current: { {{YearToDate}} }, previous: { {{LastYear}} }) { countChange }
                }
              }
              """,
            User);

        GraphQlTestContext.Data(json, "orderStatistics");
        _context.Statistics.Calls.Select(x => x.FromDate).Should().BeEquivalentTo([_yearToDateFrom, _lastYearFrom]);
    }

    [Fact]
    public async Task PeriodAndComparison_ReturnFiguresInTheContextCurrency()
    {
        _context.Statistics.Result = criteria => criteria.FromDate switch
        {
            var from when from == _yearToDateFrom => Statistics(criteria, total: 600m, count: 3, average: 200m),
            var from when from == _lastYearFrom => Statistics(criteria, total: 200m, count: 2, average: 100m),
            _ => Statistics(criteria, total: 0m, count: 0, average: 0m),
        };

        var json = await _context.ExecuteAsync(
            $$"""
              query {
                orderStatistics(storeId: "B2B-store", currencyCode: "USD", cultureName: "en-US") {
                  currencyCode
                  ytd: period({{YearToDate}}) {
                    total { amount currency { code } }
                    count
                    average { amount }
                    excludedCount
                    excludedCurrencies
                  }
                  ytdVsLastYear: comparison(current: { {{YearToDate}} }, previous: { {{LastYear}} }) {
                    totalChange { amount }
                    totalChangePercent
                    countChange
                    countChangePercent
                    averageChange { amount }
                    averageChangePercent
                  }
                  fromNothing: comparison(current: { {{YearToDate}} }, previous: { {{LastMonth}} }) {
                    totalChange { amount }
                    totalChangePercent
                    countChangePercent
                  }
                }
              }
              """,
            User);

        var statistics = GraphQlTestContext.Data(json, "orderStatistics");

        var ytd = statistics.GetProperty("ytd");
        Amount(ytd, "total").Should().Be(600m);
        ytd.GetProperty("total").GetProperty("currency").GetProperty("code").GetString().Should().Be("USD");
        ytd.GetProperty("count").GetInt32().Should().Be(3);
        Amount(ytd, "average").Should().Be(200m);
        ytd.GetProperty("excludedCount").GetInt32().Should().Be(1);
        ytd.GetProperty("excludedCurrencies").EnumerateArray().Select(x => x.GetString()).Should().Equal("GBP");

        var comparison = statistics.GetProperty("ytdVsLastYear");
        Amount(comparison, "totalChange").Should().Be(400m);
        comparison.GetProperty("totalChangePercent").GetDecimal().Should().Be(200m);
        comparison.GetProperty("countChange").GetInt32().Should().Be(1);
        comparison.GetProperty("countChangePercent").GetDecimal().Should().Be(50m);
        Amount(comparison, "averageChange").Should().Be(100m);
        comparison.GetProperty("averageChangePercent").GetDecimal().Should().Be(100m);

        // From an empty previous period the change is the whole current figure, and no percentage exists.
        var fromNothing = statistics.GetProperty("fromNothing");
        Amount(fromNothing, "totalChange").Should().Be(600m);
        fromNothing.GetProperty("totalChangePercent").ValueKind.Should().Be(JsonValueKind.Null);
        fromNothing.GetProperty("countChangePercent").ValueKind.Should().Be(JsonValueKind.Null);
    }

    private static string Query(string arguments)
    {
        return $$"""
                 query {
                   orderStatistics({{arguments}}) {
                     currencyCode
                     ytd: period({{YearToDate}}) { count }
                   }
                 }
                 """;
    }

    private static OrderStatistics Statistics(OrderStatisticsCriteria criteria, decimal total, int count, decimal average)
    {
        return new OrderStatistics
        {
            CurrencyCode = criteria.CurrencyCode,
            Total = total,
            Count = count,
            Average = average,
            ExcludedCount = 1,
            ExcludedCurrencies = ["GBP"],
        };
    }

    private static decimal Amount(JsonElement parent, string field)
    {
        return parent.GetProperty(field).GetProperty("amount").GetDecimal();
    }
}

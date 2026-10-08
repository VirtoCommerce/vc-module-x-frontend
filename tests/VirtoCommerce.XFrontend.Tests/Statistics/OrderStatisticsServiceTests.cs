using System;
using System.Threading.Tasks;
using FluentAssertions;
using VirtoCommerce.XFrontend.Core.Statistics.Models;
using VirtoCommerce.XFrontend.Data.Statistics.Services;
using VirtoCommerce.XFrontend.Tests.Infrastructure;
using Xunit;

namespace VirtoCommerce.XFrontend.Tests.Statistics;

/// <summary>
/// The aggregation over the real Orders repository on SQLite, so every filter goes through real EF translation. The
/// cache is switched off (lifetime 0) so each call reaches the database; caching is covered by
/// <see cref="OrderStatisticsCacheTests"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class OrderStatisticsServiceTests : IDisposable
{
    private const string Customer = "customer-1";
    private const string OtherCustomer = "customer-2";

    private static readonly DateTime _feb2026 = new(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _apr2026 = new(2026, 4, 21, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _jun2026 = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly SqliteOrderDatabase _database = new();
    private readonly RecordingPlatformMemoryCache _cache = new();

    public void Dispose()
    {
        _cache.Dispose();
        _database.Dispose();
    }

    [Fact]
    public async Task GetAsync_ExcludesCancelledAndPrototypeOrders()
    {
        _database.SeedOrder(Customer, 100m, _feb2026);
        _database.SeedOrder(Customer, 200m, _feb2026, isCancelled: true);
        _database.SeedOrder(Customer, 400m, _feb2026, isPrototype: true);

        var statistics = await CreateService().GetAsync(Criteria());

        statistics.Total.Should().Be(100m);
        statistics.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetAsync_CountsOnlyTheCustomersOwnOrders()
    {
        _database.SeedOrder(Customer, 100m, _feb2026);
        _database.SeedOrder(OtherCustomer, 200m, _feb2026);

        var statistics = await CreateService().GetAsync(Criteria());

        statistics.Total.Should().Be(100m);
        statistics.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetAsync_EmptyOrganizationList_MatchesNothing()
    {
        // An empty list is a scope that names no organization, not a missing filter: it must never widen to "all".
        _database.SeedOrder(Customer, 100m, _feb2026, organizationId: "org-1");
        _database.SeedOrder(Customer, 200m, _feb2026, organizationId: null);

        var criteria = Criteria();
        criteria.OrganizationIds = [];

        var statistics = await CreateService().GetAsync(criteria);

        statistics.Count.Should().Be(0);
        statistics.Total.Should().Be(0m);
    }

    [Fact]
    public async Task GetAsync_NullOrganizationList_DoesNotFilterByOrganization()
    {
        _database.SeedOrder(Customer, 100m, _feb2026, organizationId: "org-1");
        _database.SeedOrder(Customer, 200m, _feb2026, organizationId: "org-2");
        _database.SeedOrder(Customer, 400m, _feb2026, organizationId: null);

        var statistics = await CreateService().GetAsync(Criteria());

        statistics.Total.Should().Be(700m);
        statistics.Count.Should().Be(3);
    }

    [Fact]
    public async Task GetAsync_OrganizationList_KeepsOnlyTheListedOrganizations()
    {
        _database.SeedOrder(Customer, 100m, _feb2026, organizationId: "org-1");
        _database.SeedOrder(Customer, 200m, _feb2026, organizationId: "org-2");
        _database.SeedOrder(Customer, 400m, _feb2026, organizationId: null);

        var criteria = Criteria();
        criteria.OrganizationIds = ["org-1"];

        var statistics = await CreateService().GetAsync(criteria);

        statistics.Total.Should().Be(100m);
        statistics.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetAsync_FiltersByStore()
    {
        _database.SeedOrder(Customer, 100m, _feb2026, storeId: "B2B-store");
        _database.SeedOrder(Customer, 200m, _feb2026, storeId: "Electronics");

        var criteria = Criteria();
        criteria.StoreId = "B2B-store";

        var statistics = await CreateService().GetAsync(criteria);

        statistics.Total.Should().Be(100m);
        statistics.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetAsync_FiltersByStatuses()
    {
        _database.SeedOrder(Customer, 100m, _feb2026, status: "New");
        _database.SeedOrder(Customer, 200m, _feb2026, status: "Processing");
        _database.SeedOrder(Customer, 400m, _feb2026, status: "Completed");

        var criteria = Criteria();
        criteria.Statuses = ["New", "Completed"];

        var statistics = await CreateService().GetAsync(criteria);

        statistics.Total.Should().Be(500m);
        statistics.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetAsync_EmptyStatusList_MatchesNothing()
    {
        _database.SeedOrder(Customer, 100m, _feb2026, status: "New");

        var criteria = Criteria();
        criteria.Statuses = [];

        var statistics = await CreateService().GetAsync(criteria);

        statistics.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetAsync_DateBoundsAreInclusive()
    {
        // An order at exactly 'from' or exactly 'to' counts; one second outside either bound does not.
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        _database.SeedOrder(Customer, 100m, from);
        _database.SeedOrder(Customer, 200m, to);
        _database.SeedOrder(Customer, 400m, from.AddSeconds(-1));
        _database.SeedOrder(Customer, 800m, to.AddSeconds(1));

        var criteria = Criteria();
        criteria.FromDate = from;
        criteria.ToDate = to;

        var statistics = await CreateService().GetAsync(criteria);

        statistics.Total.Should().Be(300m);
        statistics.Count.Should().Be(2);
        statistics.FirstOrderDate.Should().Be(from);
        statistics.LastOrderDate.Should().Be(to);
    }

    [Fact]
    public async Task GetAsync_OpenBounds_DoNotFilter()
    {
        _database.SeedOrder(Customer, 100m, _feb2026);
        _database.SeedOrder(Customer, 200m, _jun2026);

        var onlyFrom = Criteria();
        onlyFrom.FromDate = _apr2026;
        var onlyTo = Criteria();
        onlyTo.ToDate = _apr2026;

        var service = CreateService();

        (await service.GetAsync(onlyFrom)).Total.Should().Be(200m);
        (await service.GetAsync(onlyTo)).Total.Should().Be(100m);
    }

    [Fact]
    public async Task GetAsync_FoldsCurrencies_IntoTheRequestedCurrency()
    {
        // 100 USD + 100 EUR (1 EUR = 1.25 USD) = 225 USD over 2 orders: the average needs each group's count kept
        // until after conversion.
        _database.SeedOrder(Customer, 100m, _feb2026, currency: "USD");
        _database.SeedOrder(Customer, 100m, _apr2026, currency: "EUR");

        var service = CreateService();
        var usd = await service.GetAsync(Criteria());

        usd.CurrencyCode.Should().Be("USD");
        usd.Total.Should().Be(225m);
        usd.Count.Should().Be(2);
        usd.Average.Should().Be(112.5m);

        var eurCriteria = Criteria();
        eurCriteria.CurrencyCode = "EUR";
        var eur = await service.GetAsync(eurCriteria);

        eur.CurrencyCode.Should().Be("EUR");
        eur.Total.Should().Be(180m); // 100 USD = 80 EUR, plus 100 EUR
    }

    [Fact]
    public async Task GetAsync_UnconfiguredCurrency_IsExcludedFromEveryFigure_AndReported()
    {
        // GBP is not configured, so its orders cannot be converted: they must not move any figure - not even the
        // first order date, although the GBP order is the oldest - and must be reported instead of silently dropped.
        _database.SeedOrder(Customer, 999m, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), currency: "GBP");
        _database.SeedOrder(Customer, 1m, _jun2026, currency: "GBP");
        _database.SeedOrder(Customer, 100m, _feb2026, currency: "USD");
        _database.SeedOrder(Customer, 200m, _apr2026, currency: "USD");

        var statistics = await CreateService().GetAsync(Criteria());

        statistics.Total.Should().Be(300m);
        statistics.Count.Should().Be(2);
        statistics.FirstOrderDate.Should().Be(_feb2026);
        statistics.LastOrderDate.Should().Be(_apr2026);
        statistics.ExcludedCount.Should().Be(2);
        statistics.ExcludedCurrencies.Should().Equal("GBP");
    }

    [Fact]
    public async Task GetAsync_AllCurrenciesConfigured_ReportsNothingExcluded()
    {
        _database.SeedOrder(Customer, 100m, _feb2026, currency: "USD");
        _database.SeedOrder(Customer, 100m, _feb2026, currency: "EUR");

        var statistics = await CreateService().GetAsync(Criteria());

        statistics.ExcludedCount.Should().Be(0);
        statistics.ExcludedCurrencies.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_Average_IsTotalDividedByCount_RoundedToTheCurrency()
    {
        _database.SeedOrder(Customer, 100m, _feb2026);
        _database.SeedOrder(Customer, 100m, _apr2026);
        _database.SeedOrder(Customer, 101m, _jun2026);

        var statistics = await CreateService().GetAsync(Criteria());

        statistics.Total.Should().Be(301m);
        statistics.Average.Should().Be(100.33m); // 301 / 3 = 100.333...
    }

    [Fact]
    public async Task GetAsync_NoOrders_ReturnsZeros()
    {
        _database.SeedOrder(OtherCustomer, 100m, _feb2026);

        var statistics = await CreateService().GetAsync(Criteria());

        statistics.CurrencyCode.Should().Be("USD");
        statistics.Total.Should().Be(0m);
        statistics.Count.Should().Be(0);
        statistics.Average.Should().Be(0m);
        statistics.FirstOrderDate.Should().BeNull();
        statistics.LastOrderDate.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_UnknownCurrency_IsAClearError()
    {
        _database.SeedOrder(Customer, 100m, _feb2026);

        var criteria = Criteria();
        criteria.CurrencyCode = "XYZ";

        var action = () => CreateService().GetAsync(criteria);

        (await action.Should().ThrowAsync<ArgumentException>()).Which.Message.Should().Contain("'XYZ' is not configured");
    }

    [Fact]
    public async Task GetByOrganizationAsync_GroupsTheSameAggregationPerOrganization()
    {
        _database.SeedOrder(Customer, 100m, _feb2026, organizationId: "org-1");
        _database.SeedOrder(Customer, 100m, _apr2026, organizationId: "org-1", currency: "EUR");
        _database.SeedOrder(Customer, 200m, _jun2026, organizationId: "org-2");
        _database.SeedOrder(Customer, 999m, _jun2026, organizationId: "org-2", isCancelled: true);
        _database.SeedOrder(Customer, 400m, _jun2026, organizationId: null);
        _database.SeedOrder(OtherCustomer, 800m, _jun2026, organizationId: "org-1");

        var byOrganization = await CreateService().GetByOrganizationAsync(Criteria());

        // An order without an organization belongs to no group; another customer's order is out of scope.
        byOrganization.Keys.Should().BeEquivalentTo("org-1", "org-2");

        byOrganization["org-1"].Total.Should().Be(225m);
        byOrganization["org-1"].Count.Should().Be(2);
        byOrganization["org-1"].FirstOrderDate.Should().Be(_feb2026);
        byOrganization["ORG-2"].Total.Should().Be(200m); // keys compare case-insensitively, like every id lookup
        byOrganization["org-2"].Count.Should().Be(1);
    }

    [Fact]
    public async Task GetByOrganizationAsync_AppliesTheOrganizationList()
    {
        _database.SeedOrder(Customer, 100m, _feb2026, organizationId: "org-1");
        _database.SeedOrder(Customer, 200m, _feb2026, organizationId: "org-2");

        var criteria = Criteria();
        criteria.OrganizationIds = ["org-2"];

        var byOrganization = await CreateService().GetByOrganizationAsync(criteria);

        byOrganization.Should().ContainSingle().Which.Key.Should().Be("org-2");

        criteria.OrganizationIds = [];
        (await CreateService().GetByOrganizationAsync(criteria)).Should().BeEmpty();
    }

    private OrderStatisticsService CreateService()
    {
        return new OrderStatisticsService(_database.CreateRepository, new TestCurrencyService(), _cache, TestSettingsManager.WithOrderCacheExpiration(0));
    }

    private static OrderStatisticsCriteria Criteria()
    {
        return new OrderStatisticsCriteria
        {
            CustomerId = Customer,
            CurrencyCode = "USD",
        };
    }
}

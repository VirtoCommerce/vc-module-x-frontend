using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Data.Model;
using VirtoCommerce.OrdersModule.Data.Repositories;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.XFrontend.Core.Statistics.Models;
using VirtoCommerce.XFrontend.Data.Statistics.Services;
using VirtoCommerce.XFrontend.Tests.Infrastructure;
using Xunit;

namespace VirtoCommerce.XFrontend.Tests.Statistics;

/// <summary>
/// The statistics cache: entries are keyed by the full criteria, carry the platform's per-customer order token (the
/// one Orders' <c>CustomerOrderService.ClearCache</c> expires on every save and delete) and live for the configured
/// minutes. Loads are counted through the <c>BuildQuery</c> seam, which runs once per database aggregation.
/// </summary>
[Trait("Category", "Unit")]
public sealed class OrderStatisticsCacheTests : IDisposable
{
    private static readonly DateTime _feb2026 = new(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _apr2026 = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

    // The order token is process-wide static state: a fixed customer id expired here would also evict the entries
    // of a test running in parallel, so every test uses customer ids of its own.
    private readonly string _customer = Guid.NewGuid().ToString("N");
    private readonly string _otherCustomer = Guid.NewGuid().ToString("N");

    private readonly SqliteOrderDatabase _database = new();

    public void Dispose()
    {
        _database.Dispose();
    }

    [Fact]
    public async Task RepeatedCall_IsServedFromTheCache()
    {
        using var cache = new RecordingPlatformMemoryCache();
        var service = CreateService(cache, minutes: 5);
        _database.SeedOrder(_customer, 100m, _feb2026);

        (await service.GetAsync(Criteria(_customer))).Total.Should().Be(100m);

        // A row written behind the Orders services' back expires no token, so the cached figure stands.
        _database.SeedOrder(_customer, 500m, _feb2026);

        (await service.GetAsync(Criteria(_customer))).Total.Should().Be(100m);
        service.Loads.Should().Be(1);
    }

    [Fact]
    public async Task Customers_NeverShareAnEntry()
    {
        using var cache = new RecordingPlatformMemoryCache();
        var service = CreateService(cache, minutes: 5);
        _database.SeedOrder(_customer, 100m, _feb2026);
        _database.SeedOrder(_otherCustomer, 200m, _feb2026);

        (await service.GetAsync(Criteria(_customer))).Total.Should().Be(100m);

        // The key carries the customer id: a key without it would hand the first customer's cached 100 to the second.
        (await service.GetAsync(Criteria(_otherCustomer))).Total.Should().Be(200m);
        service.Loads.Should().Be(2);
    }

    [Fact]
    public async Task AnotherCustomersOrderChange_DoesNotEvict()
    {
        using var cache = new RecordingPlatformMemoryCache();
        var service = CreateService(cache, minutes: 5);
        _database.SeedOrder(_customer, 100m, _feb2026);
        var otherOrderId = _database.SeedOrder(_otherCustomer, 200m, _feb2026);

        await service.GetAsync(Criteria(_customer));

        ClearOrderCache(otherOrderId, _otherCustomer);

        await service.GetAsync(Criteria(_customer));
        service.Loads.Should().Be(1);
    }

    [Fact]
    public async Task OwnOrderChange_Evicts()
    {
        using var cache = new RecordingPlatformMemoryCache();
        var service = CreateService(cache, minutes: 5);
        _database.SeedOrder(_customer, 100m, _feb2026);

        (await service.GetAsync(Criteria(_customer))).Total.Should().Be(100m);

        var orderId = _database.SeedOrder(_customer, 50m, _feb2026);
        ClearOrderCache(orderId, _customer);

        (await service.GetAsync(Criteria(_customer))).Total.Should().Be(150m);
        service.Loads.Should().Be(2);
    }

    [Fact]
    public async Task OrderChangedWhileLoading_IsNotMissed()
    {
        // The token is taken before the load: an order saved while the aggregation runs expires the entry being
        // built. A token taken after the load would be fresh, and the stale figure would stay cached for its lifetime.
        using var cache = new RecordingPlatformMemoryCache();
        var service = CreateService(cache, minutes: 5);
        var orderId = _database.SeedOrder(_customer, 100m, _feb2026);

        service.OnLoad = () =>
        {
            service.OnLoad = null;
            ClearOrderCache(orderId, _customer);
        };

        await service.GetAsync(Criteria(_customer));
        await service.GetAsync(Criteria(_customer));

        service.Loads.Should().Be(2);
    }

    [Fact]
    public async Task ReturnedResults_AreCopiesOfTheCachedOnes()
    {
        // The cache keeps one instance per key: a caller changing what it got must not change what the next caller gets.
        using var cache = new RecordingPlatformMemoryCache();
        var service = CreateService(cache, minutes: 5);
        _database.SeedOrder(_customer, 100m, _feb2026);
        _database.SeedOrder(_customer, 999m, _feb2026, currency: "GBP");

        var first = await service.GetAsync(Criteria(_customer));
        first.Total = 1m;
        first.ExcludedCurrencies.Add("XXX");

        var second = await service.GetAsync(Criteria(_customer));
        second.Total.Should().Be(100m);
        second.ExcludedCurrencies.Should().Equal("GBP");

        var firstByOrganization = await service.GetByOrganizationAsync(Criteria(_customer));
        firstByOrganization["org-1"].Total = 1m;
        firstByOrganization.Remove("org-1");

        var secondByOrganization = await service.GetByOrganizationAsync(Criteria(_customer));
        secondByOrganization["org-1"].Total.Should().Be(100m);

        // One load per method: the second calls were cache hits, so the copies came from the cached instances.
        service.Loads.Should().Be(2);
    }

    [Fact]
    public async Task Entry_ExpiresAfterTheSettingMinutes_WithoutSliding()
    {
        // The platform default here is sliding (as in a stock appsettings.json); the statistics entry must not slide,
        // or a busy dashboard would keep a figure alive forever.
        using var cache = new RecordingPlatformMemoryCache(new CachingOptions { CacheSlidingExpiration = TimeSpan.FromMinutes(15) });
        var service = CreateService(cache, minutes: 5);
        _database.SeedOrder(_customer, 100m, _feb2026);

        await service.GetAsync(Criteria(_customer));

        var entry = cache.Entries.Should().ContainSingle().Subject;
        entry.SlidingExpiration.Should().BeNull();
        entry.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task ZeroMinutes_BypassesTheCache()
    {
        using var cache = new RecordingPlatformMemoryCache();
        var service = CreateService(cache, minutes: 0);
        _database.SeedOrder(_customer, 100m, _feb2026);

        await service.GetAsync(Criteria(_customer));
        await service.GetAsync(Criteria(_customer));

        service.Loads.Should().Be(2);
        cache.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task PlatformCacheDisabled_IsHonored()
    {
        // With caching disabled platform-wide the default entry lives one tick; the statistics lifetime must not extend it.
        using var cache = new RecordingPlatformMemoryCache(new CachingOptions { CacheEnabled = false });
        var service = CreateService(cache, minutes: 5);
        _database.SeedOrder(_customer, 100m, _feb2026);

        await service.GetAsync(Criteria(_customer));
        await service.GetAsync(Criteria(_customer));

        service.Loads.Should().Be(2);
    }

    [Fact]
    public async Task DifferentCriteria_AndMethods_AreDifferentEntries()
    {
        using var cache = new RecordingPlatformMemoryCache();
        var service = CreateService(cache, minutes: 5);
        _database.SeedOrder(_customer, 100m, _feb2026);
        _database.SeedOrder(_customer, 200m, _apr2026);

        var february = Criteria(_customer);
        february.ToDate = _feb2026;
        var april = Criteria(_customer);
        april.FromDate = _apr2026;

        (await service.GetAsync(february)).Total.Should().Be(100m);
        (await service.GetAsync(april)).Total.Should().Be(200m);
        (await service.GetByOrganizationAsync(february)).Should().ContainKey("org-1");

        service.Loads.Should().Be(3);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task CriteriaWithoutCustomer_IsRefused(string customerId)
    {
        // The entries are invalidated per customer: an organization-only criteria would have no token to expire.
        using var cache = new RecordingPlatformMemoryCache();
        var service = CreateService(cache, minutes: 5);

        var getAction = () => service.GetAsync(Criteria(customerId));
        var byOrganizationAction = () => service.GetByOrganizationAsync(Criteria(customerId));

        (await getAction.Should().ThrowAsync<ArgumentException>()).Which.Message.Should().Contain("CustomerId is required");
        (await byOrganizationAction.Should().ThrowAsync<ArgumentException>()).Which.Message.Should().Contain("CustomerId is required");
        service.Loads.Should().Be(0);
    }

    private static void ClearOrderCache(string orderId, string customerId)
    {
        // Exactly what Orders' CustomerOrderService.ClearCache does for every saved or deleted order.
        GenericSearchCachingRegion<CustomerOrder>.ExpireRegion();
        GenericCachingRegion<CustomerOrder>.ExpireTokenForKey(orderId);
        GenericCachingRegion<CustomerOrder>.ExpireTokenForKey(customerId);
    }

    private CountingOrderStatisticsService CreateService(IPlatformMemoryCache cache, int minutes)
    {
        return new CountingOrderStatisticsService(_database.CreateRepository, new TestCurrencyService(), cache, TestSettingsManager.WithOrderCacheExpiration(minutes));
    }

    private static OrderStatisticsCriteria Criteria(string customerId)
    {
        return new OrderStatisticsCriteria
        {
            CustomerId = customerId,
            CurrencyCode = "USD",
        };
    }

    /// <summary>Counts database aggregations through the <c>BuildQuery</c> seam and can act in the middle of one.</summary>
    private sealed class CountingOrderStatisticsService : OrderStatisticsService
    {
        private int _loads;

        public CountingOrderStatisticsService(Func<IOrderRepository> repositoryFactory, ICurrencyService currencyService, IPlatformMemoryCache platformMemoryCache, ISettingsManager settingsManager)
            : base(repositoryFactory, currencyService, platformMemoryCache, settingsManager)
        {
        }

        public int Loads => _loads;

        public Action OnLoad { get; set; }

        protected override IQueryable<CustomerOrderEntity> BuildQuery(IOrderRepository repository, OrderStatisticsCriteria criteria)
        {
            Interlocked.Increment(ref _loads);
            OnLoad?.Invoke();

            return base.BuildQuery(repository, criteria);
        }
    }
}

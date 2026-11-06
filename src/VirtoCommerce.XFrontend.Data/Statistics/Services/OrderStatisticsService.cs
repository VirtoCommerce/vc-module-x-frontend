using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Data.Model;
using VirtoCommerce.OrdersModule.Data.Repositories;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.XFrontend.Core;
using VirtoCommerce.XFrontend.Core.Statistics.Helpers;
using VirtoCommerce.XFrontend.Core.Statistics.Models;
using VirtoCommerce.XFrontend.Core.Statistics.Services;

namespace VirtoCommerce.XFrontend.Data.Statistics.Services;

public class OrderStatisticsService : IOrderStatisticsService
{
    private readonly Func<IOrderRepository> _repositoryFactory;
    private readonly ICurrencyService _currencyService;
    private readonly IPlatformMemoryCache _platformMemoryCache;
    private readonly ISettingsManager _settingsManager;

    public OrderStatisticsService(
        Func<IOrderRepository> repositoryFactory,
        ICurrencyService currencyService,
        IPlatformMemoryCache platformMemoryCache,
        ISettingsManager settingsManager)
    {
        _repositoryFactory = repositoryFactory;
        _currencyService = currencyService;
        _platformMemoryCache = platformMemoryCache;
        _settingsManager = settingsManager;
    }

    public virtual async Task<OrderStatistics> GetAsync(OrderStatisticsCriteria criteria)
    {
        EnsureCustomerScope(criteria);

        var expirationMinutes = await _settingsManager.GetValueAsync<int>(ModuleConstants.Settings.Statistics.OrderCacheExpirationMinutes);
        if (expirationMinutes <= 0)
        {
            return await LoadAsync(criteria);
        }

        var cacheKey = CacheKey.With(GetType(), nameof(GetAsync), criteria.GetCacheKey());
        var statistics = await _platformMemoryCache.GetOrCreateExclusiveAsync(cacheKey, async options =>
        {
            // Attached before the load, so a change saved while it runs expires the entry instead of being missed.
            options.AddExpirationToken(CreateChangeToken(criteria));
            options.AbsoluteExpirationRelativeToNow = ShorterOf(options.AbsoluteExpirationRelativeToNow, TimeSpan.FromMinutes(expirationMinutes));
            options.SlidingExpiration = null;

            return await LoadAsync(criteria);
        });

        return statistics.CloneTyped();
    }

    // The platform default may be shorter, e.g. when caching is disabled platform-wide.
    private static TimeSpan ShorterOf(TimeSpan? configured, TimeSpan expiration)
    {
        return configured.HasValue && configured.Value < expiration ? configured.Value : expiration;
    }

    protected virtual void EnsureCustomerScope(OrderStatisticsCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        if (string.IsNullOrEmpty(criteria.CustomerId))
        {
            throw new ArgumentException("CustomerId is required: order statistics are scoped to one customer and cached per customer.", nameof(criteria));
        }
    }

    protected virtual IChangeToken CreateChangeToken(OrderStatisticsCriteria criteria)
    {
        // Orders' CustomerOrderService.ClearCache expires this token on every save or delete of the customer's orders.
        return GenericCachingRegion<CustomerOrder>.CreateChangeTokenForKey(criteria.CustomerId);
    }

    protected virtual async Task<OrderStatistics> LoadAsync(OrderStatisticsCriteria criteria)
    {
        var currencies = (await _currencyService.GetAllCurrenciesAsync()).ToList();
        var currency = GetCurrency(currencies, criteria.CurrencyCode);

        using var repository = _repositoryFactory();

        var rows = await BuildQuery(repository, criteria)
            .GroupBy(x => x.Currency)
            .Select(g => new
            {
                Currency = g.Key,
                Total = g.Sum(x => x.Total),
                Count = g.Count(),
                FirstDate = g.Min(x => x.CreatedDate),
                LastDate = g.Max(x => x.CreatedDate),
            })
            .ToListAsync();

        var aggregates = rows
            .Select(x => CreateAggregate(x.Currency, x.Total, x.Count, x.FirstDate, x.LastDate))
            .ToList();

        return ToStatistics(aggregates, currency, currencies);
    }

    protected virtual IQueryable<CustomerOrderEntity> BuildQuery(IOrderRepository repository, OrderStatisticsCriteria criteria)
    {
        var query = repository.CustomerOrders.Where(x => !x.IsPrototype && !x.IsCancelled && x.CustomerId == criteria.CustomerId);

        if (criteria.OrganizationIds != null)
        {
            query = query.Where(x => criteria.OrganizationIds.Contains(x.OrganizationId));
        }

        if (!string.IsNullOrEmpty(criteria.StoreId))
        {
            query = query.Where(x => x.StoreId == criteria.StoreId);
        }

        if (criteria.FromDate != null)
        {
            query = query.Where(x => x.CreatedDate >= criteria.FromDate);
        }

        if (criteria.ToDate != null)
        {
            query = query.Where(x => x.CreatedDate <= criteria.ToDate);
        }

        return query;
    }

    protected virtual OrderStatistics ToStatistics(IList<CurrencyAggregate> aggregates, Currency currency, IList<Currency> currencies)
    {
        return CurrencyFold.Fold(aggregates, currency, currencies);
    }

    protected virtual Currency GetCurrency(IList<Currency> currencies, string currencyCode)
    {
        return currencies.FirstOrDefault(x => x.Code.EqualsIgnoreCase(currencyCode))
            ?? throw new ArgumentException($"Currency '{currencyCode}' is not configured.", nameof(currencyCode));
    }

    protected virtual CurrencyAggregate CreateAggregate(string currencyCode, decimal total, int count, DateTime firstDate, DateTime lastDate)
    {
        var aggregate = AbstractTypeFactory<CurrencyAggregate>.TryCreateInstance();
        aggregate.CurrencyCode = currencyCode;
        aggregate.Total = total;
        aggregate.Count = count;
        aggregate.FirstDate = firstDate;
        aggregate.LastDate = lastDate;

        return aggregate;
    }
}

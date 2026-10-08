using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
using VirtoCommerce.XFrontend.Core.Statistics.Extensions;
using VirtoCommerce.XFrontend.Core.Statistics.Helpers;
using VirtoCommerce.XFrontend.Core.Statistics.Models;
using VirtoCommerce.XFrontend.Core.Statistics.Services;
using VirtoCommerce.XFrontend.Data.Statistics.Caching;

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

        var cacheKey = CacheKey.With(GetType(), nameof(GetAsync), criteria.GetCacheKey());
        var expirationMinutes = await _settingsManager.GetValueAsync<int>(ModuleConstants.Settings.Statistics.OrderCacheExpirationMinutes);

        var statistics = await StatisticsCache.GetOrCreateAsync(_platformMemoryCache, cacheKey, expirationMinutes, () => CreateChangeToken(criteria), () => LoadAsync(criteria));

        return statistics.CloneTyped();
    }

    public virtual async Task<IDictionary<string, OrderStatistics>> GetByOrganizationAsync(OrderStatisticsCriteria criteria)
    {
        EnsureCustomerScope(criteria);

        var cacheKey = CacheKey.With(GetType(), nameof(GetByOrganizationAsync), criteria.GetCacheKey());
        var expirationMinutes = await _settingsManager.GetValueAsync<int>(ModuleConstants.Settings.Statistics.OrderCacheExpirationMinutes);

        var byOrganization = await StatisticsCache.GetOrCreateAsync(_platformMemoryCache, cacheKey, expirationMinutes, () => CreateChangeToken(criteria), () => LoadByOrganizationAsync(criteria));

        return byOrganization.ToDictionary(x => x.Key, x => x.Value.CloneTyped(), StringComparer.OrdinalIgnoreCase);
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

    protected virtual async Task<IDictionary<string, OrderStatistics>> LoadByOrganizationAsync(OrderStatisticsCriteria criteria)
    {
        var currencies = (await _currencyService.GetAllCurrenciesAsync()).ToList();
        var currency = GetCurrency(currencies, criteria.CurrencyCode);

        using var repository = _repositoryFactory();

        var rows = await BuildQuery(repository, criteria)
            .Where(x => x.OrganizationId != null)
            .GroupBy(x => new { x.OrganizationId, x.Currency })
            .Select(g => new
            {
                g.Key.OrganizationId,
                g.Key.Currency,
                Total = g.Sum(x => x.Total),
                Count = g.Count(),
                FirstDate = g.Min(x => x.CreatedDate),
                LastDate = g.Max(x => x.CreatedDate),
            })
            .ToListAsync();

        return rows
            .GroupBy(x => x.OrganizationId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => ToStatistics(g.Select(x => CreateAggregate(x.Currency, x.Total, x.Count, x.FirstDate, x.LastDate)).ToList(), currency, currencies),
                StringComparer.OrdinalIgnoreCase);
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

        if (criteria.Statuses != null)
        {
            query = query.Where(x => criteria.Statuses.Contains(x.Status));
        }

        return query.WhereBetween(x => x.CreatedDate, criteria.FromDate, criteria.ToDate);
    }

    protected virtual OrderStatistics ToStatistics(IList<CurrencyAggregate> aggregates, Currency currency, IList<Currency> currencies)
    {
        var folded = CurrencyFold.Fold(aggregates, currency, currencies);

        var result = AbstractTypeFactory<OrderStatistics>.TryCreateInstance();
        result.CurrencyCode = folded.CurrencyCode;
        result.Total = folded.Total;
        result.Count = folded.Count;
        result.Average = folded.Average;
        result.FirstOrderDate = folded.FirstDate;
        result.LastOrderDate = folded.LastDate;
        result.ExcludedCount = folded.ExcludedCount;
        result.ExcludedCurrencies = folded.ExcludedCurrencies;

        return result;
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

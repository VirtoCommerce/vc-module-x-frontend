using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XFrontend.Core.Statistics.Models;
using VirtoCommerce.XFrontend.Core.Statistics.Queries;
using VirtoCommerce.XFrontend.Core.Statistics.Services;

namespace VirtoCommerce.XFrontend.Data.Statistics.Queries;

public class OrderStatisticsQueryHandler : IQueryHandler<OrderStatisticsQuery, OrderStatisticsContext>
{
    private readonly IStatisticsCurrencyResolver _currencyResolver;

    public OrderStatisticsQueryHandler(IStatisticsCurrencyResolver currencyResolver)
    {
        _currencyResolver = currencyResolver;
    }

    public virtual async Task<OrderStatisticsContext> Handle(OrderStatisticsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.CustomerId))
        {
            return null;
        }

        var result = AbstractTypeFactory<OrderStatisticsContext>.TryCreateInstance();
        result.CustomerId = request.CustomerId;
        result.OrganizationIds = string.IsNullOrEmpty(request.OrganizationId) ? null : [request.OrganizationId];
        // An empty store id means all stores, like null: one scope, so one DataLoader key and one cache entry.
        result.StoreId = string.IsNullOrEmpty(request.StoreId) ? null : request.StoreId;
        result.CurrencyCode = await _currencyResolver.ResolveCurrencyCodeAsync(request.CurrencyCode, result.StoreId)
            ?? throw new ExecutionError(GetUnresolvedCurrencyMessage(request.CurrencyCode, result.StoreId));

        return result;
    }

    protected virtual string GetUnresolvedCurrencyMessage(string currencyCode, string storeId)
    {
        if (!string.IsNullOrEmpty(currencyCode))
        {
            return $"Currency '{currencyCode}' is not configured.";
        }

        return string.IsNullOrEmpty(storeId)
            ? "No currency was requested and no primary currency is configured."
            : $"No currency was requested and no configured currency could be resolved for store '{storeId}'.";
    }
}

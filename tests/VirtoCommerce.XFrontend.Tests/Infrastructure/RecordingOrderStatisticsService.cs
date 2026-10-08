using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.XFrontend.Core.Statistics.Models;
using VirtoCommerce.XFrontend.Core.Statistics.Services;

namespace VirtoCommerce.XFrontend.Tests.Infrastructure;

/// <summary>
/// Stands in for the statistics service behind the GraphQL layer: records every criteria it receives (the GraphQL
/// tests assert what scope reached the service, and how many times) and answers with <see cref="Result"/>.
/// </summary>
public sealed class RecordingOrderStatisticsService : IOrderStatisticsService
{
    public ConcurrentQueue<OrderStatisticsCriteria> Calls { get; } = new();

    public Func<OrderStatisticsCriteria, OrderStatistics> Result { get; set; } = criteria => new OrderStatistics { CurrencyCode = criteria.CurrencyCode };

    public Task<OrderStatistics> GetAsync(OrderStatisticsCriteria criteria)
    {
        Calls.Enqueue(criteria);

        return Task.FromResult(Result(criteria));
    }

    public Task<IDictionary<string, OrderStatistics>> GetByOrganizationAsync(OrderStatisticsCriteria criteria)
    {
        throw new NotSupportedException();
    }
}

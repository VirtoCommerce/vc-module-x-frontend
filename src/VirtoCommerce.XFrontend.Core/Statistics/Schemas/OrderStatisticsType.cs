using System;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XFrontend.Core.Statistics.Helpers;
using VirtoCommerce.XFrontend.Core.Statistics.Models;
using VirtoCommerce.XFrontend.Core.Statistics.Services;

namespace VirtoCommerce.XFrontend.Core.Statistics.Schemas;

public class OrderStatisticsType : ExtendableGraphType<OrderStatisticsContext>
{
    private readonly IDataLoaderContextAccessor _dataLoaderContextAccessor;

    public OrderStatisticsType(IDataLoaderContextAccessor dataLoaderContextAccessor)
    {
        _dataLoaderContextAccessor = dataLoaderContextAccessor;

        Name = "OrderStatistics";

        Field(x => x.CurrencyCode, nullable: false).Description("Currency all amounts are converted to.");

        Field<NonNullGraphType<OrderStatisticsPeriodType>>("period")
            .Description("Figures for the orders created in a date range; all orders when both bounds are omitted.")
            .Argument<DateTimeGraphType>("from", "Inclusive lower bound of the order creation date.")
            .Argument<DateTimeGraphType>("to", "Inclusive upper bound of the order creation date.")
            .Resolve(context =>
            {
                var from = context.GetArgument<DateTime?>("from");
                var to = context.GetArgument<DateTime?>("to");

                return GetPeriodLoader(context).LoadAsync((from, to));
            });

        Field<NonNullGraphType<OrderStatisticsComparisonType>>("comparison")
            .Description("Change from the previous period to the current one, computed over the same convertible orders as `period` (see `period.excludedCount` for what was left out).")
            .Argument<NonNullGraphType<InputStatisticsPeriodType>>("current", "The later period.")
            .Argument<NonNullGraphType<InputStatisticsPeriodType>>("previous", "The period to compare against.")
            .Resolve(context =>
            {
                var current = context.GetArgument<StatisticsPeriod>("current");
                var previous = context.GetArgument<StatisticsPeriod>("previous");
                var loader = GetPeriodLoader(context);

                // Both loads are queued before chaining, so they share one batch with any period over the same range.
                var currentResult = loader.LoadAsync((current.From, current.To));
                var previousResult = loader.LoadAsync((previous.From, previous.To));

                return currentResult.Then(currentStatistics => previousResult.Then(previousStatistics => BuildComparison(currentStatistics, previousStatistics)));
            });
    }

    protected virtual OrderStatisticsCriteria CreateCriteria(OrderStatisticsContext source, DateTime? from, DateTime? to)
    {
        var criteria = AbstractTypeFactory<OrderStatisticsCriteria>.TryCreateInstance();
        criteria.CustomerId = source.CustomerId;
        criteria.OrganizationIds = source.OrganizationIds;
        criteria.StoreId = source.StoreId;
        criteria.CurrencyCode = source.CurrencyCode;
        criteria.FromDate = from;
        criteria.ToDate = to;

        return criteria;
    }

    protected virtual OrderStatisticsComparison BuildComparison(OrderStatistics current, OrderStatistics previous)
    {
        var result = AbstractTypeFactory<OrderStatisticsComparison>.TryCreateInstance();
        result.CurrencyCode = current.CurrencyCode;
        result.TotalChange = current.Total - previous.Total;
        result.TotalChangePercent = StatisticsMath.ChangePercent(previous.Total, current.Total);
        result.CountChange = current.Count - previous.Count;
        result.CountChangePercent = StatisticsMath.ChangePercent(previous.Count, current.Count);
        result.AverageChange = current.Average - previous.Average;
        result.AverageChangePercent = StatisticsMath.ChangePercent(previous.Average, current.Average);

        return result;
    }

    protected virtual IDataLoader<(DateTime? From, DateTime? To), OrderStatistics> GetPeriodLoader(IResolveFieldContext<OrderStatisticsContext> context)
    {
        // The batch runs after this resolver returns, when GraphQL may have reused the field context: capture values only.
        var source = context.Source;
        var statisticsService = context.RequestServices.GetRequiredService<IOrderStatisticsService>();

        // One loader per scope and request, with the range as the batch key: each range is aggregated once.
        var loaderKey = $"{nameof(OrderStatisticsType)}:{CreateCriteria(source, from: null, to: null).GetCacheKey()}";

        return _dataLoaderContextAccessor.Context.GetOrAddBatchLoader<(DateTime? From, DateTime? To), OrderStatistics>(loaderKey, async ranges =>
        {
            var results = await Task.WhenAll(ranges.Select(async range =>
            {
                var statistics = await statisticsService.GetAsync(CreateCriteria(source, range.From, range.To));

                return (range, statistics);
            }));

            return results.ToDictionary(x => x.range, x => x.statistics);
        });
    }
}

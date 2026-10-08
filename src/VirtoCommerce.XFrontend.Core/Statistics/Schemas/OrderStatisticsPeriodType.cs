using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XFrontend.Core.Statistics.Models;

namespace VirtoCommerce.XFrontend.Core.Statistics.Schemas;

public class OrderStatisticsPeriodType : ExtendableGraphType<OrderStatistics>
{
    public OrderStatisticsPeriodType()
    {
        Name = "OrderStatisticsPeriod";

        Field<NonNullGraphType<MoneyType>>("total")
            .Description("Sum of the order totals.")
            .ResolveAsync(async context => await MoneyResolver.ResolveAsync(context, context.Source.CurrencyCode, context.Source.Total));

        Field(x => x.Count, nullable: false).Description("Number of orders.");

        Field<NonNullGraphType<MoneyType>>("average")
            .Description("Average order total.")
            .ResolveAsync(async context => await MoneyResolver.ResolveAsync(context, context.Source.CurrencyCode, context.Source.Average));

        Field(x => x.FirstOrderDate, nullable: true).Description("Creation date of the earliest order.");
        Field(x => x.LastOrderDate, nullable: true).Description("Creation date of the latest order.");
        Field(x => x.ExcludedCount, nullable: false).Description("Number of orders left out of every figure because their currency is not configured, so it cannot be converted.");

        Field<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>("excludedCurrencies")
            .Description("Currencies of the orders left out.")
            .Resolve(context => context.Source.ExcludedCurrencies);
    }
}

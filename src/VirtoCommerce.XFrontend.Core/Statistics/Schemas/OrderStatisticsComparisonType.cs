using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XFrontend.Core.Statistics.Models;

namespace VirtoCommerce.XFrontend.Core.Statistics.Schemas;

public class OrderStatisticsComparisonType : ExtendableGraphType<OrderStatisticsComparison>
{
    public OrderStatisticsComparisonType()
    {
        Name = "OrderStatisticsComparison";

        Field<NonNullGraphType<MoneyType>>("totalChange")
            .Description("Current total minus previous total.")
            .Resolve(context => context.Source.TotalChange.ToMoney(context.GetCurrencyByCode(context.Source.CurrencyCode)));

        Field(x => x.TotalChangePercent, nullable: true).Description("Change of the total in percent; null when the previous total is zero.");
        Field(x => x.CountChange, nullable: false).Description("Current order count minus previous order count.");
        Field(x => x.CountChangePercent, nullable: true).Description("Change of the order count in percent; null when the previous count is zero.");

        Field<NonNullGraphType<MoneyType>>("averageChange")
            .Description("Current average minus previous average.")
            .Resolve(context => context.Source.AverageChange.ToMoney(context.GetCurrencyByCode(context.Source.CurrencyCode)));

        Field(x => x.AverageChangePercent, nullable: true).Description("Change of the average in percent; null when the previous average is zero.");
    }
}

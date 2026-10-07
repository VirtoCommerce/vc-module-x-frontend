using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XFrontend.Core.Statistics.Models;

namespace VirtoCommerce.XFrontend.Core.Statistics.Queries;

public class OrderStatisticsQuery : Query<OrderStatisticsContext>
{
    public string CustomerId { get; set; }

    public string OrganizationId { get; set; }

    public string StoreId { get; set; }

    public string CurrencyCode { get; set; }

    public string CultureName { get; set; }

    public override IEnumerable<QueryArgument> GetArguments()
    {
        yield return Argument<StringGraphType>(nameof(StoreId), "Store whose orders are counted (all stores when omitted).");
        yield return Argument<StringGraphType>(nameof(CurrencyCode), "Currency all amounts are converted to (the store's default currency, then the platform's primary currency, when omitted).");
        yield return Argument<StringGraphType>(nameof(CultureName), "Culture for the formatted money amounts (\"en-US\").");
    }

    public override void Map(IResolveFieldContext context)
    {
        StoreId = context.GetArgument<string>(nameof(StoreId));
        CurrencyCode = context.GetArgument<string>(nameof(CurrencyCode));
        CultureName = context.GetArgument<string>(nameof(CultureName));
        CustomerId = context.GetCurrentUserId();
        OrganizationId = context.GetCurrentOrganizationId();
    }
}

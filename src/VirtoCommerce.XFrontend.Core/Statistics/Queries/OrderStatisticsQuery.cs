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

    /// <summary>Every order of the current organization instead of the caller's own (needs the organization order permission).</summary>
    public bool OrganizationScope { get; set; }

    /// <summary>With <see cref="OrganizationScope"/>: one member's orders, by contact id.</summary>
    public string MemberId { get; set; }

    public override IEnumerable<QueryArgument> GetArguments()
    {
        yield return Argument<StringGraphType>(nameof(StoreId), "Store whose orders are counted (all stores when omitted).");
        yield return Argument<StringGraphType>(nameof(CurrencyCode), "Currency all amounts are converted to (the store's default currency, then the platform's primary currency, when omitted).");
        yield return Argument<StringGraphType>(nameof(CultureName), "Culture for the formatted money amounts (\"en-US\").");
        yield return Argument<BooleanGraphType>(nameof(OrganizationScope), "Count every order of the current organization, whoever placed it, instead of the caller's own. Needs the xapi:my_organization:order:view permission.");
        yield return Argument<StringGraphType>(nameof(MemberId), "With organizationScope: only the orders of this member (a contact id) of the current organization.");
    }

    public override void Map(IResolveFieldContext context)
    {
        StoreId = context.GetArgument<string>(nameof(StoreId));
        CurrencyCode = context.GetArgument<string>(nameof(CurrencyCode));
        CultureName = context.GetArgument<string>(nameof(CultureName));
        OrganizationScope = context.GetArgument<bool?>(nameof(OrganizationScope)) ?? false;
        MemberId = context.GetArgument<string>(nameof(MemberId));
        CustomerId = context.GetCurrentUserId();
        OrganizationId = context.GetCurrentOrganizationId();
    }
}

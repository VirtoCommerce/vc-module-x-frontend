using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XFrontend.Core.Models;

namespace VirtoCommerce.XFrontend.Core.Queries;

public class PageContextQuery : Query<PageContexResponse>
{
    public string Domain { get; set; }

    public string CultureName { get; set; }

    public string Permalink { get; set; }

    // optional?
    public string OrganizationId { get; set; } // whitelabeling, slugInfo

    public string UserId { get; set; } // whitelabeling, get me, slugInfo
    public string UserName { get; set; }
    public bool IsAnonymous { get; set; }

    public string StoreId { get; set; } // store, whitelabeling, slugInfo

    public override IEnumerable<QueryArgument> GetArguments()
    {
        yield return Argument<StringGraphType>(nameof(Domain));
        yield return Argument<StringGraphType>(nameof(CultureName));
        yield return Argument<StringGraphType>(nameof(Permalink));

        yield return Argument<StringGraphType>(nameof(OrganizationId));
        yield return Argument<StringGraphType>(nameof(UserId));
        yield return Argument<StringGraphType>(nameof(StoreId));
    }

    public override void Map(IResolveFieldContext context)
    {
        Domain = context.GetArgument<string>(nameof(Domain));
        CultureName = context.GetArgument<string>(nameof(CultureName));
        Permalink = context.GetArgument<string>(nameof(Permalink));

        OrganizationId = context.GetArgument<string>(nameof(OrganizationId)) ?? context.GetCurrentOrganizationId();
        //UserId = context.GetArgument<string>(nameof(UserId)) ?? context.GetCurrentUserId();
        UserId = context.GetArgument<string>(nameof(UserId));

        StoreId = context.GetArgument<string>(nameof(StoreId));
    }
}

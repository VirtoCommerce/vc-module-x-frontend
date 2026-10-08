using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XFrontend.Core.Layouts.Models;

namespace VirtoCommerce.XFrontend.Core.Layouts.Queries;

public class LayoutQuery : Query<Layout>
{
    public string Scope { get; set; }

    public string StoreId { get; set; }

    public string UserId { get; set; }

    public override IEnumerable<QueryArgument> GetArguments()
    {
        yield return Argument<NonNullGraphType<StringGraphType>>(nameof(Scope), "Dashboard the layout belongs to (\"accountDashboard\").");
        yield return Argument<StringGraphType>(nameof(StoreId), "Store the layout was saved for.");
    }

    public override void Map(IResolveFieldContext context)
    {
        Scope = context.GetArgument<string>(nameof(Scope));
        StoreId = context.GetArgument<string>(nameof(StoreId));
        UserId = context.GetCurrentUserId();
    }
}

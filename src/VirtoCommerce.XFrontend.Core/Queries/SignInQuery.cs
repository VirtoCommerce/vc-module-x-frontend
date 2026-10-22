using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.BaseQueries;

namespace VirtoCommerce.XFrontend.Core.Queries;

public abstract class SignInQuery<TResult> : Query<TResult>
{
    public ApplicationUser User { get; set; }

    public override IEnumerable<QueryArgument> GetArguments()
    {
        yield break;
    }

    public override void Map(IResolveFieldContext context)
    {
    }
}

using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Security.Authorization;
using VirtoCommerce.XFrontend.Core.Queries;

namespace VirtoCommerce.XFrontend.Data.Queries;

public abstract class SignInQueryBuilder<TQuery, TResult, TResultGraphType>(
    IAuthorizationService authorizationService,
    Func<UserManager<ApplicationUser>> userManagerFactory)
    : QueryBuilder<TQuery, TResult, TResultGraphType>
        (authorizationService)
    where TQuery : SignInQuery<TResult>
    where TResultGraphType : IGraphType
{
    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, TQuery request)
    {
        if (!context.IsAuthenticated())
        {
            throw AuthorizationError.AnonymousAccessDenied();
        }

        request.User = await userManagerFactory().FindByIdAsync(context.GetCurrentUserId());

        await base.BeforeMediatorSend(context, request);
    }
}

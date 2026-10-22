using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Security.Extensions;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Security.Authorization;
using VirtoCommerce.XFrontend.Core.Commands;

namespace VirtoCommerce.XFrontend.Data.Commands;

public abstract class SignInKeyCommandBuilder<TCommand, TResult, TCommandGraphType, TResultGraphType>(
    IAuthorizationService authorizationService,
    Func<UserManager<ApplicationUser>> userManagerFactory)
    : CommandBuilder<TCommand, TResult, TCommandGraphType, TResultGraphType>
        (authorizationService)
    where TCommand : SignInKeyCommand<TResult>
    where TCommandGraphType : IInputObjectGraphType
    where TResultGraphType : IGraphType
{
    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, TCommand request)
    {
        if (!context.IsAuthenticated())
        {
            throw AuthorizationError.AnonymousAccessDenied();
        }

        if (context.GetCurrentPrincipal().IsImpersonated())
        {
            throw AuthorizationError.Forbidden();
        }

        request.User = await userManagerFactory().FindByIdAsync(context.GetCurrentUserId());

        if (request.User is null)
        {
            throw AuthorizationError.AnonymousAccessDenied();
        }

        await base.BeforeMediatorSend(context, request);
    }
}

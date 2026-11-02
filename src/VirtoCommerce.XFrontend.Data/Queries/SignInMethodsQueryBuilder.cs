using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Security.SignInMethods;
using VirtoCommerce.Platform.Security.Extensions;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XFrontend.Core.Queries;
using VirtoCommerce.XFrontend.Core.Schemas;

namespace VirtoCommerce.XFrontend.Data.Queries;

public class SignInMethodsQueryBuilder(
    IAuthorizationService authorizationService,
    Func<UserManager<ApplicationUser>> userManagerFactory)
    : SignInQueryBuilder<SignInMethodsQuery, IList<SignInMethod>, ListGraphType<NonNullGraphType<SignInMethodType>>>
        (authorizationService, userManagerFactory)
{
    protected override string Name => "signInMethods";

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, SignInMethodsQuery request)
    {
        request.IsImpersonated = context.GetCurrentPrincipal().IsImpersonated();

        await base.BeforeMediatorSend(context, request);
    }
}

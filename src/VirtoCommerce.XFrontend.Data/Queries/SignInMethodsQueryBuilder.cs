using System;
using System.Collections.Generic;
using GraphQL.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Security.SignInMethods;
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
}

using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Schemas;
using VirtoCommerce.XFrontend.Core.Commands;
using VirtoCommerce.XFrontend.Core.Schemas;

namespace VirtoCommerce.XFrontend.Data.Commands;

public class RenameSignInKeyCommandBuilder(
    IAuthorizationService authorizationService,
    Func<UserManager<ApplicationUser>> userManagerFactory)
    : SignInKeyCommandBuilder<RenameSignInKeyCommand, IdentityResult, InputRenameSignInKeyType, IdentityResultType>
        (authorizationService, userManagerFactory)
{
    protected override string Name => "renameSignInKey";
}

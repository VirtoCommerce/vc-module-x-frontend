using Microsoft.AspNetCore.Identity;

namespace VirtoCommerce.XFrontend.Core.Commands;

public class DeleteSignInKeyCommand : SignInKeyCommand<IdentityResult>
{
    public string SignInMethodName { get; set; }

    public string KeyId { get; set; }
}

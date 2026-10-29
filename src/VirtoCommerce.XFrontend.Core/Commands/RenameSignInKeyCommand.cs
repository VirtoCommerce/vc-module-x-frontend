using Microsoft.AspNetCore.Identity;

namespace VirtoCommerce.XFrontend.Core.Commands;

public class RenameSignInKeyCommand : SignInKeyCommand<IdentityResult>
{
    public string SignInMethodName { get; set; }

    public string KeyId { get; set; }

    public string Name { get; set; }
}

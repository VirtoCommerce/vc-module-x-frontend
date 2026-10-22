using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.XFrontend.Core.Commands;

public abstract class SignInKeyCommand<TResult> : ICommand<TResult>
{
    public ApplicationUser User { get; set; }
}

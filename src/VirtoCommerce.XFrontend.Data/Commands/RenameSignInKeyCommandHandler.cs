using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core.Security.SignInMethods;
using VirtoCommerce.XFrontend.Core.Commands;

namespace VirtoCommerce.XFrontend.Data.Commands;

public class RenameSignInKeyCommandHandler(ISignInMethodService signInMethodService)
    : IRequestHandler<RenameSignInKeyCommand, IdentityResult>
{
    public virtual Task<IdentityResult> Handle(RenameSignInKeyCommand request, CancellationToken cancellationToken)
    {
        return signInMethodService.RenameKeyAsync(request.User, request.SignInMethodName, request.KeyId, request.Name);
    }
}

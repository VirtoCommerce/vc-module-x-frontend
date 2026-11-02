using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core.Security.SignInMethods;
using VirtoCommerce.XFrontend.Core.Commands;

namespace VirtoCommerce.XFrontend.Data.Commands;

public class DeleteSignInKeyCommandHandler(ISignInMethodService signInMethodService)
    : IRequestHandler<DeleteSignInKeyCommand, IdentityResult>
{
    public virtual Task<IdentityResult> Handle(DeleteSignInKeyCommand request, CancellationToken cancellationToken)
    {
        return signInMethodService.DeleteKeyAsync(request.User, request.SignInMethodName, request.KeyId);
    }
}

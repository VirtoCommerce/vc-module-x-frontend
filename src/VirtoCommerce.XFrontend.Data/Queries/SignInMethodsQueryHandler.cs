using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Security.SignInMethods;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XFrontend.Core.Queries;

namespace VirtoCommerce.XFrontend.Data.Queries;

public class SignInMethodsQueryHandler(
    ISignInMethodService signInMethodService)
    : IQueryHandler<SignInMethodsQuery, IList<SignInMethod>>
{
    public virtual async Task<IList<SignInMethod>> Handle(SignInMethodsQuery request, CancellationToken cancellationToken)
    {
        var signInMethods = await signInMethodService.GetSignInMethodsAsync(request.User);

        if (request.IsImpersonated)
        {
            foreach (var signInMethod in signInMethods)
            {
                signInMethod.CanCreateKey = false;
                signInMethod.CanRenameKey = false;
                signInMethod.CanDeleteKey = false;
            }
        }

        return signInMethods;
    }
}

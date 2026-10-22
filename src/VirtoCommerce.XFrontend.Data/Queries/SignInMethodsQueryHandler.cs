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
        return await signInMethodService.GetSignInMethodsAsync(request.User);
    }
}

using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XFrontend.Core;
using VirtoCommerce.XFrontend.Core.Layouts.Models;
using VirtoCommerce.XFrontend.Core.Layouts.Queries;
using VirtoCommerce.XFrontend.Core.Layouts.Services;

namespace VirtoCommerce.XFrontend.Data.Layouts.Queries;

public class LayoutQueryHandler : IQueryHandler<LayoutQuery, Layout>
{
    private readonly ILayoutService _layoutService;

    public LayoutQueryHandler(ILayoutService layoutService)
    {
        _layoutService = layoutService;
    }

    public virtual Task<Layout> Handle(LayoutQuery request, CancellationToken cancellationToken)
    {
        // saveLayout rejects such a key, so nothing was ever saved under it; reading it could only alias another key.
        if (!IsValidKey(request.Scope, request.StoreId))
        {
            return Task.FromResult<Layout>(null);
        }

        return _layoutService.GetLayoutAsync(request.UserId, request.Scope, request.StoreId);
    }

    protected virtual bool IsValidKey(string scope, string storeId)
    {
        return !string.IsNullOrEmpty(scope)
            && Regex.IsMatch(scope, ModuleConstants.Layouts.ScopePattern)
            && (string.IsNullOrEmpty(storeId) || Regex.IsMatch(storeId, ModuleConstants.Layouts.StoreIdPattern));
    }
}

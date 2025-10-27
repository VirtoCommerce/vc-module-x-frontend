using System.Security.Claims;
using System.Threading.Tasks;
using GraphQL;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XFrontend.Core.Models;
using VirtoCommerce.XFrontend.Core.Queries;
using VirtoCommerce.XFrontend.Core.Schemas;

namespace VirtoCommerce.XFrontend.Data.Queries;

class PageContextQueryBuilder : QueryBuilder<PageContextQuery, PageContexResponse, PageContexResponseType>
{
    protected override string Name => "pageContext";

    public PageContextQueryBuilder(IMediator mediator, IAuthorizationService authorizationService)
        : base(mediator, authorizationService)
    {
    }

    protected override Task BeforeMediatorSend(IResolveFieldContext<object> context, PageContextQuery request)
    {
        var principal = context.GetCurrentPrincipal();
        request.UserName = principal?.Identity?.Name;

        if (request.UserName.IsNullOrEmpty())
        {
            request.IsAnonymous = true;
            request.UserId ??= principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        return base.BeforeMediatorSend(context, request);
    }
}

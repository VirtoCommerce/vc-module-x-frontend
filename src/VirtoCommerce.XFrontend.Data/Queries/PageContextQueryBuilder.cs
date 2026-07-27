using System;
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

public class PageContextQueryBuilder : QueryBuilder<PageContextQuery, PageContextResponse, PageContextResponseType>
{
    protected override string Name => "pageContext";

    public PageContextQueryBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }

    [Obsolete("Use the constructor without IMediator. The mediator is resolved from context.RequestServices per request.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public PageContextQueryBuilder(IMediator mediator, IAuthorizationService authorizationService)
        : this(authorizationService)
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

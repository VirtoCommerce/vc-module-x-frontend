using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XFrontend.Core.Layouts.Models;
using VirtoCommerce.XFrontend.Core.Layouts.Queries;
using VirtoCommerce.XFrontend.Core.Layouts.Schemas;

namespace VirtoCommerce.XFrontend.Data.Layouts.Queries;

public class LayoutQueryBuilder : QueryBuilder<LayoutQuery, Layout, LayoutType>
{
    protected override string Name => "layout";

    public LayoutQueryBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, LayoutQuery request)
    {
        await base.BeforeMediatorSend(context, request);

        await context.RequestServices.GetRequiredService<IUserManagerCore>().CheckCurrentUserState(context, allowAnonymous: false);
    }
}

using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XFrontend.Core.Layouts.Commands;
using VirtoCommerce.XFrontend.Core.Layouts.Models;
using VirtoCommerce.XFrontend.Core.Layouts.Schemas;

namespace VirtoCommerce.XFrontend.Data.Layouts.Commands;

public class SaveLayoutCommandBuilder : CommandBuilder<SaveLayoutCommand, Layout, InputLayoutType, LayoutType>
{
    protected override string Name => "saveLayout";

    public SaveLayoutCommandBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }

    protected override SaveLayoutCommand GetRequest(IResolveFieldContext<object> context)
    {
        var request = base.GetRequest(context);
        request.UserId = context.GetCurrentUserId();

        return request;
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, SaveLayoutCommand request)
    {
        await base.BeforeMediatorSend(context, request);

        await context.RequestServices.GetRequiredService<IUserManagerCore>().CheckCurrentUserState(context, allowAnonymous: false);
    }
}

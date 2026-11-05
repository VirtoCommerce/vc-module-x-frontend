using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Security.Authorization;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XFrontend.Core.Statistics.Models;
using VirtoCommerce.XFrontend.Core.Statistics.Queries;
using VirtoCommerce.XFrontend.Core.Statistics.Schemas;
using ProfileModuleConstants = VirtoCommerce.ProfileExperienceApiModule.Data.ModuleConstants;

namespace VirtoCommerce.XFrontend.Data.Statistics.Queries;

public class OrderStatisticsQueryBuilder : QueryBuilder<OrderStatisticsQuery, OrderStatisticsContext, OrderStatisticsType>
{
    protected override string Name => "orderStatistics";

    public OrderStatisticsQueryBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, OrderStatisticsQuery request)
    {
        await base.BeforeMediatorSend(context, request);

        await context.RequestServices.GetRequiredService<IUserManagerCore>().CheckCurrentUserState(context, allowAnonymous: false);

        CheckScope(context, request);

        // The money fields format in the requested culture, from this one list.
        var currencies = await context.RequestServices.GetRequiredService<ICurrencyService>().GetAllCurrenciesAsync();
        context.SetCurrencies(currencies, request.CultureName);
    }

    /// <summary>
    /// An organization's orders are someone else's orders: only a member allowed to see all of the organization's
    /// orders may count them, and only for the organization the token was issued for. The token carries that
    /// organization's roles, so its permission claims decide. Whether <c>memberId</c> belongs to it is checked by the
    /// handler, which loads the member.
    /// </summary>
    protected virtual void CheckScope(IResolveFieldContext<object> context, OrderStatisticsQuery request)
    {
        if (!request.OrganizationScope)
        {
            if (!string.IsNullOrEmpty(request.MemberId))
            {
                throw new ExecutionError("memberId is only accepted together with organizationScope: true.");
            }

            return;
        }

        if (string.IsNullOrEmpty(request.OrganizationId))
        {
            throw AuthorizationError.Forbidden("Organization statistics need a current organization.");
        }

        var permission = ProfileModuleConstants.Security.Permissions.MyOrganizationOrderView;

        if (context.GetCurrentPrincipal()?.HasGlobalPermission(permission) != true)
        {
            throw AuthorizationError.PermissionRequired(permission);
        }
    }
}

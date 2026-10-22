using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XFrontend.Core.Statistics.Models;
using VirtoCommerce.XFrontend.Core.Statistics.Queries;
using VirtoCommerce.XFrontend.Core.Statistics.Schemas;

namespace VirtoCommerce.XFrontend.Data.Statistics.Queries;

public class OrderStatisticsQueryBuilder : QueryBuilder<OrderStatisticsQuery, OrderStatisticsContext, OrderStatisticsType>
{
    private readonly ICurrencyService _currencyService;

    protected override string Name => "orderStatistics";

    public OrderStatisticsQueryBuilder(IAuthorizationService authorizationService, ICurrencyService currencyService)
        : base(authorizationService)
    {
        _currencyService = currencyService;
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, OrderStatisticsQuery request)
    {
        await base.BeforeMediatorSend(context, request);

        await context.RequestServices.GetRequiredService<IUserManagerCore>().CheckCurrentUserState(context, allowAnonymous: false);

        // The money fields format in the requested culture, from this one list.
        context.SetCurrencies(await _currencyService.GetAllCurrenciesAsync(), request.CultureName);
    }
}

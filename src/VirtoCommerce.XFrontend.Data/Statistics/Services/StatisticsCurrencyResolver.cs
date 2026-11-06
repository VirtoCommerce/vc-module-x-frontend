using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.XFrontend.Core.Statistics.Services;

namespace VirtoCommerce.XFrontend.Data.Statistics.Services;

public class StatisticsCurrencyResolver : IStatisticsCurrencyResolver
{
    private readonly IStoreService _storeService;
    private readonly ICurrencyService _currencyService;

    public StatisticsCurrencyResolver(IStoreService storeService, ICurrencyService currencyService)
    {
        _storeService = storeService;
        _currencyService = currencyService;
    }

    public virtual async Task<string> ResolveCurrencyCodeAsync(string currencyCode, string storeId)
    {
        var code = currencyCode;

        if (string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(storeId))
        {
            var store = await _storeService.GetNoCloneAsync(storeId);
            code = store?.DefaultCurrency;
        }

        var currencies = await _currencyService.GetAllCurrenciesAsync();

        var currency = string.IsNullOrEmpty(code)
            ? currencies.FirstOrDefault(x => x.IsPrimary)
            : currencies.FirstOrDefault(x => x.Code.EqualsIgnoreCase(code));

        return currency?.Code;
    }
}

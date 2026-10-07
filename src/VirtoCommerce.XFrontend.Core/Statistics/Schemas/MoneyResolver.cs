using System.Threading.Tasks;
using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Xapi.Core.Extensions;

namespace VirtoCommerce.XFrontend.Core.Statistics.Schemas;

internal static class MoneyResolver
{
    public static async Task<Money> ResolveAsync(IResolveFieldContext context, string currencyCode, decimal amount)
    {
        var currencyService = context.RequestServices.GetRequiredService<ICurrencyService>();
        var currencies = await currencyService.GetAllCurrenciesAsync();

        return new Money(amount, currencies.GetCurrencyForLanguage(currencyCode, context.GetCultureName()));
    }
}

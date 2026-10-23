using System.Threading.Tasks;

namespace VirtoCommerce.XFrontend.Core.Statistics.Services;

public interface IStatisticsCurrencyResolver
{
    Task<string> ResolveCurrencyCodeAsync(string currencyCode, string storeId);
}

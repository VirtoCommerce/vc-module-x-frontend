using System.Collections.Generic;

namespace VirtoCommerce.XFrontend.Core.Statistics.Models;

public class CurrencyFoldResult : CurrencyAggregate
{
    public decimal Average { get; set; }

    public int ExcludedCount { get; set; }

    public IList<string> ExcludedCurrencies { get; set; } = [];
}

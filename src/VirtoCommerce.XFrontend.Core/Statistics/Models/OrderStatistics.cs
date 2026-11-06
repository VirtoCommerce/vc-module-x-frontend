using System;
using System.Collections.Generic;
using System.Linq;

namespace VirtoCommerce.XFrontend.Core.Statistics.Models;

public class OrderStatistics : ICloneable
{
    public decimal Total { get; set; }

    public int Count { get; set; }

    public decimal Average { get; set; }

    public DateTime? FirstOrderDate { get; set; }

    public DateTime? LastOrderDate { get; set; }

    public string CurrencyCode { get; set; }

    public int ExcludedCount { get; set; }

    public IList<string> ExcludedCurrencies { get; set; } = [];

    public virtual object Clone()
    {
        var result = (OrderStatistics)MemberwiseClone();
        result.ExcludedCurrencies = ExcludedCurrencies?.ToList();

        return result;
    }
}

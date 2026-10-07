using System;

namespace VirtoCommerce.XFrontend.Core.Statistics.Models;

public class CurrencyAggregate
{
    public string CurrencyCode { get; set; }

    public decimal Total { get; set; }

    public int Count { get; set; }

    public DateTime? FirstDate { get; set; }

    public DateTime? LastDate { get; set; }
}

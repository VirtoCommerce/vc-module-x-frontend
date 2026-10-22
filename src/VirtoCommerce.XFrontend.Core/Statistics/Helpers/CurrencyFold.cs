using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XFrontend.Core.Statistics.Models;

namespace VirtoCommerce.XFrontend.Core.Statistics.Helpers;

public static class CurrencyFold
{
    public static OrderStatistics Fold(IEnumerable<CurrencyAggregate> aggregates, Currency currency, IList<Currency> currencies)
    {
        var result = AbstractTypeFactory<OrderStatistics>.TryCreateInstance();
        result.CurrencyCode = currency.Code;

        var total = 0m;

        foreach (var aggregate in aggregates)
        {
            var sourceCurrency = currencies.FirstOrDefault(x => x.Code.EqualsIgnoreCase(aggregate.CurrencyCode));

            if (sourceCurrency == null)
            {
                result.ExcludedCount += aggregate.Count;
                result.ExcludedCurrencies.Add(aggregate.CurrencyCode);
                continue;
            }

            total += new Money(aggregate.Total, sourceCurrency).ConvertTo(currency).InternalAmount;
            result.Count += aggregate.Count;
            result.FirstOrderDate = Earlier(result.FirstOrderDate, aggregate.FirstDate);
            result.LastOrderDate = Later(result.LastOrderDate, aggregate.LastDate);
        }

        result.Total = Round(total, currency);
        result.Average = result.Count == 0 ? 0m : Round(total / result.Count, currency);

        return result;
    }

    private static decimal Round(decimal amount, Currency currency)
    {
        return Math.Round(amount, currency.DecimalDigits, MidpointRounding.AwayFromZero);
    }

    private static DateTime? Earlier(DateTime? current, DateTime? candidate)
    {
        return candidate != null && (current == null || candidate < current) ? candidate : current;
    }

    private static DateTime? Later(DateTime? current, DateTime? candidate)
    {
        return candidate != null && (current == null || candidate > current) ? candidate : current;
    }
}

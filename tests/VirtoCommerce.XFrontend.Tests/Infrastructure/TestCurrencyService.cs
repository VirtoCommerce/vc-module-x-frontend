using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Currency;

namespace VirtoCommerce.XFrontend.Tests.Infrastructure;

/// <summary>
/// Fixed currency source: USD is primary (rate 1) and EUR has rate 1.25, the "rate relative to primary" convention of
/// the real Currency table, so 1 EUR = 1.25 USD. GBP is deliberately absent: orders in it cannot be converted.
/// <c>RoundingPolicy</c> is set because <c>Money.Amount</c> calls it.
/// </summary>
public sealed class TestCurrencyService : ICurrencyService
{
    public Task<IEnumerable<Currency>> GetAllCurrenciesAsync()
    {
        IEnumerable<Currency> currencies =
        [
            new Currency(Language.InvariantLanguage, "USD", "US Dollar", "$", 1m) { IsPrimary = true, RoundingPolicy = new DefaultMoneyRoundingPolicy() },
            new Currency(Language.InvariantLanguage, "EUR", "Euro", "€", 1.25m) { RoundingPolicy = new DefaultMoneyRoundingPolicy() },
        ];

        return Task.FromResult(currencies);
    }

    public Task SaveChangesAsync(Currency[] currencies) => throw new NotSupportedException();

    public Task DeleteCurrenciesAsync(string[] codes) => throw new NotSupportedException();
}

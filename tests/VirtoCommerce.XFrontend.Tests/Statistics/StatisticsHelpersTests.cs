using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using VirtoCommerce.XFrontend.Core.Statistics.Helpers;
using VirtoCommerce.XFrontend.Core.Statistics.Models;
using VirtoCommerce.XFrontend.Tests.Infrastructure;
using Xunit;

namespace VirtoCommerce.XFrontend.Tests.Statistics;

/// <summary>The shared statistics helpers that consumers (sales-rep folds and comparisons) reuse directly.</summary>
[Trait("Category", "Unit")]
public class StatisticsHelpersTests
{
    [Theory]
    [InlineData(200, 600, 200)]
    [InlineData(200, 100, -50)]
    [InlineData(200, 200, 0)]
    public void ChangePercent_IsRelativeToThePreviousValue(int previous, int current, int expected)
    {
        StatisticsMath.ChangePercent(previous, current).Should().Be(expected);
    }

    [Fact]
    public void ChangePercent_FromZero_IsNull()
    {
        // There is no percentage growth from nothing; the storefront shows the absolute change instead.
        StatisticsMath.ChangePercent(0m, 100m).Should().BeNull();
    }

    [Fact]
    public async Task Fold_ConvertsEachCurrency_AndKeepsTheUnconvertibleApart()
    {
        var currencies = (await new TestCurrencyService().GetAllCurrenciesAsync()).ToList();
        var usd = currencies.Single(x => x.Code == "USD");

        var folded = CurrencyFold.Fold(
            [
                Aggregate("USD", 10.005m, 1, new DateTime(2026, 3, 1)),
                Aggregate("EUR", 100m, 2, new DateTime(2026, 2, 1)),
                Aggregate("GBP", 999m, 3, new DateTime(2020, 1, 1)),
            ],
            usd,
            currencies);

        folded.CurrencyCode.Should().Be("USD");
        folded.Total.Should().Be(135.01m); // 10.005 + 125, rounded half away from zero
        folded.Count.Should().Be(3);
        folded.Average.Should().Be(45m); // 135.005 / 3, from the unrounded total
        folded.FirstDate.Should().Be(new DateTime(2026, 2, 1));
        folded.LastDate.Should().Be(new DateTime(2026, 3, 1));
        folded.ExcludedCount.Should().Be(3);
        folded.ExcludedCurrencies.Should().Equal("GBP");
    }

    [Fact]
    public async Task Fold_OfNothing_IsZero()
    {
        var currencies = (await new TestCurrencyService().GetAllCurrenciesAsync()).ToList();

        var folded = CurrencyFold.Fold([], currencies[0], currencies);

        folded.Total.Should().Be(0m);
        folded.Count.Should().Be(0);
        folded.Average.Should().Be(0m);
        folded.FirstDate.Should().BeNull();
        folded.LastDate.Should().BeNull();
        folded.ExcludedCount.Should().Be(0);
    }

    private static CurrencyAggregate Aggregate(string currencyCode, decimal total, int count, DateTime date)
    {
        return new CurrencyAggregate
        {
            CurrencyCode = currencyCode,
            Total = total,
            Count = count,
            FirstDate = date,
            LastDate = date,
        };
    }
}

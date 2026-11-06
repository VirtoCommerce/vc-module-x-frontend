using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.XFrontend.Data.Statistics.Services;
using VirtoCommerce.XFrontend.Tests.Infrastructure;
using Xunit;

namespace VirtoCommerce.XFrontend.Tests.Statistics;

/// <summary>Currency defaulting: requested code, then the store's default currency, then the platform's primary one.</summary>
[Trait("Category", "Unit")]
public class StatisticsCurrencyResolverTests
{
    private readonly StatisticsCurrencyResolver _resolver;

    public StatisticsCurrencyResolverTests()
    {
        Store[] stores =
        [
            new Store { Id = "EuroStore", DefaultCurrency = "EUR" },
            new Store { Id = "NoDefaultStore" },
            new Store { Id = "PoundStore", DefaultCurrency = "GBP" },
        ];

        var storeService = new Mock<IStoreService>();
        storeService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((IList<string> ids, string _, bool _) => stores.Where(x => ids.Contains(x.Id)).ToList());

        _resolver = new StatisticsCurrencyResolver(storeService.Object, new TestCurrencyService());
    }

    [Theory]
    [InlineData("EUR", "EuroStore", "EUR")]
    [InlineData("usd", "EuroStore", "USD")] // the configured code is returned, so "usd" and "USD" share one cache key
    [InlineData(null, "EuroStore", "EUR")]
    [InlineData(null, "NoDefaultStore", "USD")]
    [InlineData(null, "UnknownStore", "USD")]
    [InlineData(null, null, "USD")]
    [InlineData("", "", "USD")]
    public async Task ResolvesRequestedThenStoreDefaultThenPrimary(string currencyCode, string storeId, string expected)
    {
        (await _resolver.ResolveCurrencyCodeAsync(currencyCode, storeId)).Should().Be(expected);
    }

    [Theory]
    [InlineData("XYZ", null)]
    [InlineData("XYZ", "EuroStore")] // an unknown requested code does not fall back to the store default
    [InlineData(null, "PoundStore")] // the store default is not a configured currency
    public async Task UnconfiguredCurrency_ResolvesToNull(string currencyCode, string storeId)
    {
        // The resolver only answers; the GraphQL handler turns the null into the error the client sees.
        (await _resolver.ResolveCurrencyCodeAsync(currencyCode, storeId)).Should().BeNull();
    }
}

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using VirtoCommerce.XFrontend.Core.Layouts.Commands;
using VirtoCommerce.XFrontend.Core.Layouts.Models;
using VirtoCommerce.XFrontend.Data.Layouts.Validation;
using Xunit;

namespace VirtoCommerce.XFrontend.Tests.Layouts;

/// <summary>
/// The saveLayout input rules. Every mutation from a signed-in buyer reaches the preference table, so the shape and
/// the size are capped; the ids and keys the storefront sends today must all pass.
/// </summary>
[Trait("Category", "Unit")]
public class SaveLayoutCommandValidatorTests
{
    private readonly SaveLayoutCommandValidator _validator = new();

    [Fact]
    public void LayoutTheStorefrontSendsToday_IsValid()
    {
        // The shape of a sales-rep dashboard layout saved by the storefront (regions, block ids, row caps, a tab
        // rule named after an order status with a space in it) and of the planned account dashboard.
        var command = Command(
            Region("statistics", Block("new_orders"), Block("active_carts"), Block("orders_placed_week"), Block("orders_placed_mtd"), Block("orders_placed_ytd"), Block("my_customers")),
            Region("mainLeft",
                Block("orders", ("maxRows", 5), ("tab.Payment required", false)),
                Block("top_sellers", ("maxRows", 5)),
                Block("recent_orders", ("maxRows", 5))),
            Region("mainRight", Block("documents", ("maxRows", 5)), Block("tasks", ("maxRows", 10))));

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("accountDashboard")]
    [InlineData("salesRepDashboard")]
    [InlineData("salesRepCustomerProfile")]
    [InlineData("a")]
    public void Scope_LettersAndDigitsStartingWithALetter_IsValid(string scope)
    {
        var command = Command(Region("mainLeft"));
        command.Scope = scope;

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("account.dashboard")] // the preference name joins its parts with dots: "a.b" would read store "b" of scope "a"
    [InlineData("1dashboard")]
    [InlineData("account-dashboard")]
    [InlineData("account dashboard")]
    [InlineData("accountDashboard\n")] // $ would match before a final newline; the patterns end with \z
    [InlineData("")]
    [InlineData(null)]
    public void Scope_Invalid_IsRejected(string scope)
    {
        var command = Command(Region("mainLeft"));
        command.Scope = scope;

        ErrorsOf(command).Should().Contain(nameof(SaveLayoutCommand.Scope));
    }

    [Fact]
    public void Scope_LongerThan64_IsRejected()
    {
        var command = Command(Region("mainLeft"));

        command.Scope = new string('a', 64);
        _validator.Validate(command).IsValid.Should().BeTrue();

        command.Scope = new string('a', 65);
        ErrorsOf(command).Should().Contain(nameof(SaveLayoutCommand.Scope));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("B2B-store")]
    [InlineData("store_1")]
    public void StoreId_OmittedOrAStoreModuleId_IsValid(string storeId)
    {
        var command = Command(Region("mainLeft"));
        command.StoreId = storeId;

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("B2B store")]
    [InlineData("b2b.store")]
    [InlineData("store/1")]
    public void StoreId_OutsideTheStoreModuleCharset_IsRejected(string storeId)
    {
        var command = Command(Region("mainLeft"));
        command.StoreId = storeId;

        ErrorsOf(command).Should().Contain(nameof(SaveLayoutCommand.StoreId));
    }

    [Fact]
    public void StoreId_LongerThan128_IsRejected()
    {
        var command = Command(Region("mainLeft"));
        command.StoreId = new string('s', 129);

        ErrorsOf(command).Should().Contain(nameof(SaveLayoutCommand.StoreId));
    }

    [Fact]
    public void RegionId_Invalid_IsRejected()
    {
        var command = Command(Region("main left"));

        ErrorsOf(command).Should().Contain("Regions[0].Id");
    }

    [Fact]
    public void BlockIdAndType_Invalid_AreRejected()
    {
        var block = Block("orders");
        block.Id = "orders!";
        block.Type = new string('t', 65);

        var command = Command(Region("mainLeft", block));

        ErrorsOf(command).Should().Equal("Regions[0].Blocks[0].Id", "Regions[0].Blocks[0].Type");
    }

    [Fact]
    public void TooManyRegions_IsRejected()
    {
        var atLimit = Command(Enumerable.Range(0, 20).Select(x => Region($"r{x}")).ToArray());
        _validator.Validate(atLimit).IsValid.Should().BeTrue();

        var overLimit = Command(Enumerable.Range(0, 21).Select(x => Region($"r{x}")).ToArray());
        ErrorsOf(overLimit).Should().Contain(nameof(SaveLayoutCommand.Regions));
    }

    [Fact]
    public void TooManyBlocksInARegion_IsRejected()
    {
        var atLimit = Command(Region("mainLeft", Enumerable.Range(0, 50).Select(x => Block($"b{x}")).ToArray()));
        _validator.Validate(atLimit).IsValid.Should().BeTrue();

        var overLimit = Command(Region("mainLeft", Enumerable.Range(0, 51).Select(x => Block($"b{x}")).ToArray()));
        ErrorsOf(overLimit).Should().Contain("Regions[0].Blocks");
    }

    [Fact]
    public void TooManySettingsInABlock_IsRejected()
    {
        var atLimit = Command(Region("mainLeft", Block("orders", Enumerable.Range(0, 50).Select(x => ($"k{x}", (object)x)).ToArray())));
        _validator.Validate(atLimit).IsValid.Should().BeTrue();

        var overLimit = Command(Region("mainLeft", Block("orders", Enumerable.Range(0, 51).Select(x => ($"k{x}", (object)x)).ToArray())));
        ErrorsOf(overLimit).Should().Contain("Regions[0].Blocks[0].Settings");
    }

    [Fact]
    public void SettingKey_EmptyOrLongerThan64_IsRejected()
    {
        var command = Command(Region("mainLeft", Block("orders", ("", 1), (new string('k', 65), 1), (new string('k', 64), 1))));

        ErrorsOf(command).Should().Equal("Regions[0].Blocks[0].Settings[0].Key", "Regions[0].Blocks[0].Settings[1].Key");
    }

    [Fact]
    public void SettingValue_LargerThan4KB_IsRejected()
    {
        // Measured as JSON: a string serializes with its two quotes.
        var command = Command(Region("mainLeft", Block("orders", ("fits", new string('v', 4094)), ("tooBig", new string('v', 4095)))));

        ErrorsOf(command).Should().Equal("Regions[0].Blocks[0].Settings[1].Value");
    }

    [Fact]
    public void LayoutLargerThan64KB_IsRejected_EvenWhenEveryPartIsWithinItsLimits()
    {
        var value = new string('v', 1000);
        var regions = Enumerable.Range(0, 2)
            .Select(r => Region($"r{r}", Enumerable.Range(0, 40).Select(b => Block($"b{b}", ("text", value))).ToArray()))
            .ToArray();

        ErrorsOf(Command(regions)).Should().Equal(nameof(SaveLayoutCommand.Regions));
    }

    private List<string> ErrorsOf(SaveLayoutCommand command)
    {
        return _validator.Validate(command).Errors.Select(x => x.PropertyName).Distinct().ToList();
    }

    private static SaveLayoutCommand Command(params LayoutRegion[] regions)
    {
        return new SaveLayoutCommand
        {
            Scope = "salesRepDashboard",
            StoreId = "B2B-store",
            SchemaVersion = 1,
            Regions = regions,
            UserId = "user-1",
        };
    }

    private static LayoutRegion Region(string id, params LayoutBlock[] blocks)
    {
        return new LayoutRegion { Id = id, Blocks = blocks };
    }

    private static LayoutBlock Block(string id, params (string Key, object Value)[] settings)
    {
        return new LayoutBlock
        {
            Id = id,
            Type = id,
            Settings = settings.Select(x => new LayoutSetting { Key = x.Key, Value = x.Value }).ToList(),
        };
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using VirtoCommerce.XFrontend.Core.Layouts.Models;
using VirtoCommerce.XFrontend.Data.Layouts.Services;
using VirtoCommerce.XFrontend.Tests.Infrastructure;
using Xunit;

namespace VirtoCommerce.XFrontend.Tests.Layouts;

/// <summary>Layout persistence as customer preferences: the preference name, the user it belongs to, the per-user cap and the JSON round-trip.</summary>
[Trait("Category", "Unit")]
public class LayoutServiceTests
{
    private const string User = "user-1";

    private readonly InMemoryCustomerPreferenceService _preferences = new();
    private readonly LayoutService _service;

    public LayoutServiceTests()
    {
        _service = new LayoutService(_preferences, _preferences);
    }

    [Theory]
    [InlineData(null, "Layout.accountDashboard")]
    [InlineData("", "Layout.accountDashboard")]
    [InlineData("B2B-store", "Layout.accountDashboard.B2B-store")]
    public async Task Save_StoresOnePreferencePerUserScopeAndStore(string storeId, string expectedName)
    {
        await _service.SaveLayoutAsync(User, "accountDashboard", NewLayout(), storeId);

        _preferences.Values.Keys.Should().Equal((User, expectedName));
    }

    [Fact]
    public async Task Get_ReadsOnlyTheCallersOwnPreference()
    {
        await _service.SaveLayoutAsync(User, "accountDashboard", NewLayout(), "B2B-store");

        (await _service.GetLayoutAsync("user-2", "accountDashboard", "B2B-store")).Should().BeNull();
        (await _service.GetLayoutAsync(User, "accountDashboard", "B2B-store")).Should().NotBeNull();
    }

    [Fact]
    public async Task Get_NeverSaved_ReturnsNull()
    {
        (await _service.GetLayoutAsync(User, "accountDashboard", "B2B-store")).Should().BeNull();
    }

    [Fact]
    public async Task Save_StampsTheModifiedDate_AndReturnsTheSavedLayout()
    {
        var before = DateTime.UtcNow;

        var saved = await _service.SaveLayoutAsync(User, "accountDashboard", NewLayout(), "B2B-store");

        saved.ModifiedDate.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
        (await _service.GetLayoutAsync(User, "accountDashboard", "B2B-store")).ModifiedDate.Should().Be(saved.ModifiedDate);
    }

    [Fact]
    public async Task NewLayout_BeyondTheUserCap_IsRefused()
    {
        // Every scope and store pair is a row of its own, so the rows one user can create are capped.
        for (var i = 0; i < 20; i++)
        {
            await _service.SaveLayoutAsync(User, $"scope{i}", NewLayout(), "B2B-store");
        }

        var action = () => _service.SaveLayoutAsync(User, "scope20", NewLayout(), "B2B-store");

        (await action.Should().ThrowAsync<ValidationException>()).Which.Message.Should().Contain("at most 20 layouts");
        _preferences.Values.Should().HaveCount(20);
    }

    [Fact]
    public async Task ExistingLayout_AtTheUserCap_CanStillBeReplaced()
    {
        for (var i = 0; i < 20; i++)
        {
            await _service.SaveLayoutAsync(User, $"scope{i}", NewLayout(), "B2B-store");
        }

        var replacement = NewLayout();
        replacement.SchemaVersion = 2;

        await _service.SaveLayoutAsync(User, "scope0", replacement, "B2B-store");

        (await _service.GetLayoutAsync(User, "scope0", "B2B-store")).SchemaVersion.Should().Be(2);
    }

    [Fact]
    public async Task UserCap_CountsOnlyTheUsersOwnLayouts()
    {
        // Other preferences of the same user and other users' layouts do not use up the cap.
        for (var i = 0; i < 25; i++)
        {
            await _preferences.SaveValue(User, $"OtherFeature.setting{i}", "value");
        }

        for (var i = 0; i < 20; i++)
        {
            await _service.SaveLayoutAsync("user-2", $"scope{i}", NewLayout(), "B2B-store");
        }

        for (var i = 0; i < 20; i++)
        {
            await _service.SaveLayoutAsync(User, $"scope{i}", NewLayout(), "B2B-store");
        }

        _preferences.Values.Keys.Count(x => x.UserId == User && x.Name.StartsWith("Layout.")).Should().Be(20);
    }

    [Fact]
    public async Task SettingValues_KeepTheirTypes()
    {
        // A number stays a number and a bool a bool; a date-like string stays a string, it is not parsed into a date.
        var layout = NewLayout();
        layout.Regions[0].Blocks[0].Settings =
        [
            new LayoutSetting { Key = "maxRows", Value = 5 },
            new LayoutSetting { Key = "compact", Value = true },
            new LayoutSetting { Key = "since", Value = "2026-08-13T20:05:30Z" },
        ];

        await _service.SaveLayoutAsync(User, "accountDashboard", layout, "B2B-store");
        var loaded = await _service.GetLayoutAsync(User, "accountDashboard", "B2B-store");

        var settings = loaded.Regions[0].Blocks[0].Settings.ToDictionary(x => x.Key, x => x.Value);
        settings["maxRows"].Should().Be(5L);
        settings["compact"].Should().Be(true);
        settings["since"].Should().Be("2026-08-13T20:05:30Z");
    }

    [Fact]
    public async Task Layout_PreservesDownstreamDerivedTypes_OnRoundTrip()
    {
        // A downstream module registered derived types (see TestAssemblyInitializer): a ROOT TestExtendedLayout (Theme)
        // and a nested TestExtendedLayoutBlock (ColorScheme), each with a field the base contract knows nothing about.
        var layout = new TestExtendedLayout
        {
            SchemaVersion = 1,
            Theme = "midnight",
            Regions =
            [
                new LayoutRegion
                {
                    Id = "statistics",
                    Blocks = [new TestExtendedLayoutBlock { Id = "b1", Type = "stat", ColorScheme = "dark" }],
                },
            ],
        };

        await _service.SaveLayoutAsync(User, "accountDashboard", layout, "B2B-store");
        var loaded = await _service.GetLayoutAsync(User, "accountDashboard", "B2B-store");

        // ROOT: DeserializeObject<Layout> returns the registered derived type, not the base generic argument.
        loaded.Should().BeOfType<TestExtendedLayout>().Which.Theme.Should().Be("midnight");

        // NESTED: the block element is rebuilt as its derived type too, with its extra field.
        loaded.Regions.Single().Blocks.Single().Should().BeOfType<TestExtendedLayoutBlock>().Which.ColorScheme.Should().Be("dark");
    }

    private static Layout NewLayout()
    {
        return new Layout
        {
            SchemaVersion = 1,
            Regions =
            [
                new LayoutRegion
                {
                    Id = "mainLeft",
                    Blocks = [new LayoutBlock { Id = "recent_orders", Type = "recent_orders" }],
                },
            ],
        };
    }
}

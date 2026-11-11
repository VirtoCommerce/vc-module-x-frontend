using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.XFrontend.Tests.Infrastructure;
using Xunit;

namespace VirtoCommerce.XFrontend.Tests.Layouts;

/// <summary>
/// The <c>layout</c> query and <c>saveLayout</c> mutation through the module's real schema, handlers, validator and
/// layout service, over in-memory customer preferences. The user is always the token's: nothing in the schema can
/// name another one.
/// </summary>
[Trait("Category", "Unit")]
public sealed class LayoutGraphQlTests : IDisposable
{
    private const string User = "user-1";
    private const string OtherUser = "user-2";

    private readonly GraphQlTestContext _context = new();

    public LayoutGraphQlTests()
    {
        _context.AddUser(User);
        _context.AddUser(OtherUser);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task SaveThenLoad_RoundTripsTheLayout()
    {
        var saved = GraphQlTestContext.Data(await _context.ExecuteAsync(SaveMutation("accountDashboard"), User), "saveLayout");
        saved.GetProperty("modifiedDate").ValueKind.Should().Be(JsonValueKind.String);

        _context.Preferences.Values.Keys.Should().Equal((User, "Layout.accountDashboard.B2B-store"));

        var layout = GraphQlTestContext.Data(await _context.ExecuteAsync(LoadQuery("accountDashboard"), User), "layout");

        layout.GetProperty("schemaVersion").GetInt32().Should().Be(1);
        layout.GetProperty("modifiedDate").GetString().Should().Be(saved.GetProperty("modifiedDate").GetString());

        var regions = layout.GetProperty("regions").EnumerateArray().ToList();
        regions.Select(x => x.GetProperty("id").GetString()).Should().Equal("statistics", "mainLeft");

        // Array order is render order, and hidden survives.
        var statistics = regions[0].GetProperty("blocks").EnumerateArray().ToList();
        statistics.Select(x => x.GetProperty("id").GetString()).Should().Equal("orders_placed_mtd", "avg_order_value");
        statistics[1].GetProperty("hidden").GetBoolean().Should().BeTrue();

        // AnyValue keeps the scalar kinds: a number stays a number and a bool a bool.
        var recentOrders = regions[1].GetProperty("blocks")[0];
        SettingValue(recentOrders, "maxRows").GetInt32().Should().Be(5);
        SettingValue(recentOrders, "compact").GetBoolean().Should().BeTrue();
        SettingValue(recentOrders, "tab.Payment required").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Load_NeverSaved_ReturnsNull()
    {
        var json = await _context.ExecuteAsync(LoadQuery("accountDashboard"), User);

        GraphQlTestContext.Data(json, "layout").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task AnotherUsersLayout_IsNotReadable()
    {
        await _context.ExecuteAsync(SaveMutation("accountDashboard"), User);

        var json = await _context.ExecuteAsync(LoadQuery("accountDashboard"), OtherUser);

        GraphQlTestContext.Data(json, "layout").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task EachUser_SavesAndReadsOnlyTheirOwnLayout()
    {
        // The owner is stamped from the token on save as well as on read: two users saving the same dashboard get a
        // row each, and each reads back their own.
        await _context.ExecuteAsync(SaveMutation("accountDashboard", schemaVersion: 1), User);
        await _context.ExecuteAsync(SaveMutation("accountDashboard", schemaVersion: 2), OtherUser);

        _context.Preferences.Values.Keys.Should().HaveCount(2)
            .And.Contain((User, "Layout.accountDashboard.B2B-store"))
            .And.Contain((OtherUser, "Layout.accountDashboard.B2B-store"));

        GraphQlTestContext.Data(await _context.ExecuteAsync(LoadQuery("accountDashboard"), User), "layout")
            .GetProperty("schemaVersion").GetInt32().Should().Be(1);
        GraphQlTestContext.Data(await _context.ExecuteAsync(LoadQuery("accountDashboard"), OtherUser), "layout")
            .GetProperty("schemaVersion").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task ScopeAndStore_AreIndependentKeys()
    {
        await _context.ExecuteAsync(SaveMutation("accountDashboard", storeId: "B2B-store"), User);

        GraphQlTestContext.Data(await _context.ExecuteAsync(LoadQuery("accountDashboard", storeId: "Electronics"), User), "layout")
            .ValueKind.Should().Be(JsonValueKind.Null);
        GraphQlTestContext.Data(await _context.ExecuteAsync(LoadQuery("accountDashboard", storeId: null), User), "layout")
            .ValueKind.Should().Be(JsonValueKind.Null);
        GraphQlTestContext.Data(await _context.ExecuteAsync(LoadQuery("salesRepDashboard", storeId: "B2B-store"), User), "layout")
            .ValueKind.Should().Be(JsonValueKind.Null);

        GraphQlTestContext.Data(await _context.ExecuteAsync(LoadQuery("accountDashboard", storeId: "B2B-store"), User), "layout")
            .GetProperty("regions").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task ScopeThatSaveLayoutRejects_ReadsNothing()
    {
        // "accountDashboard.B2B-store" joins into the same preference name as scope "accountDashboard" with store
        // "B2B-store"; saveLayout refuses such a scope, so the read must not alias the row saved under the valid pair.
        await _context.ExecuteAsync(SaveMutation("accountDashboard", storeId: "B2B-store"), User);

        var json = await _context.ExecuteAsync(LoadQuery("accountDashboard.B2B-store", storeId: null), User);

        GraphQlTestContext.Data(json, "layout").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task ScopeWithATrailingNewline_ReadsNothing()
    {
        // saveLayout rejects such a scope, so the read must not reach storage either - not even a row stored under it.
        _context.Preferences.Values[(User, "Layout.accountDashboard\n.B2B-store")] = """{"SchemaVersion":1,"Regions":[]}""";

        var json = await _context.ExecuteAsync(LoadQuery("accountDashboard\\n"), User);

        GraphQlTestContext.Data(json, "layout").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Save_ForAStoreThatDoesNotExist_IsRejected()
    {
        // Every store id is a separate preference row, so only real stores may get one.
        var json = await _context.ExecuteAsync(SaveMutation("accountDashboard", storeId: "NoSuchStore"), User);

        GraphQlTestContext.Errors(json).Should().ContainSingle().Which.Code.Should().Be("VALIDATION");
        _context.Preferences.Values.Should().BeEmpty();
    }

    [Fact]
    public async Task Save_ReplacesTheWholeLayout()
    {
        await _context.ExecuteAsync(SaveMutation("accountDashboard"), User);

        var replacement = """
            mutation {
              saveLayout(command: {
                scope: "accountDashboard", storeId: "B2B-store", schemaVersion: 2
                regions: [ { id: "mainLeft", blocks: [ { id: "only", type: "news", hidden: false, settings: [] } ] } ]
              }) { schemaVersion }
            }
            """;
        GraphQlTestContext.Data(await _context.ExecuteAsync(replacement, User), "saveLayout");

        var layout = GraphQlTestContext.Data(await _context.ExecuteAsync(LoadQuery("accountDashboard"), User), "layout");
        layout.GetProperty("schemaVersion").GetInt32().Should().Be(2);
        layout.GetProperty("regions").EnumerateArray().Should().ContainSingle()
            .Which.GetProperty("blocks").EnumerateArray().Select(x => x.GetProperty("id").GetString()).Should().Equal("only");
    }

    [Fact]
    public async Task Save_InvalidInput_IsRejected_AndNothingIsSaved()
    {
        // A dotted scope would address another key ("a.b" reads store "b" of scope "a"). The validator's exception
        // reaches the client as GraphQL.NET's generic field error, coded after the exception type.
        var json = await _context.ExecuteAsync(SaveMutation("account.dashboard"), User);

        GraphQlTestContext.Errors(json).Should().ContainSingle().Which.Code.Should().Be("VALIDATION");
        _context.Preferences.Values.Should().BeEmpty();
    }

    [Fact]
    public async Task Anonymous_IsDenied()
    {
        var load = await _context.ExecuteAnonymousAsync(LoadQuery("accountDashboard"));
        var save = await _context.ExecuteAnonymousAsync(SaveMutation("accountDashboard"));

        GraphQlTestContext.Errors(load).Should().ContainSingle().Which.Code.Should().Be(Constants.UnauthorizedCode);
        GraphQlTestContext.Errors(save).Should().ContainSingle().Which.Code.Should().Be(Constants.UnauthorizedCode);
        _context.Preferences.Values.Should().BeEmpty();
    }

    [Fact]
    public async Task LockedAccount_IsDenied()
    {
        _context.AddUser("locked-user", locked: true);

        var load = await _context.ExecuteAsync(LoadQuery("accountDashboard"), "locked-user");
        var save = await _context.ExecuteAsync(SaveMutation("accountDashboard"), "locked-user");

        GraphQlTestContext.Errors(load).Should().ContainSingle().Which.Code.Should().Be(Constants.UserLockedCode);
        GraphQlTestContext.Errors(save).Should().ContainSingle().Which.Code.Should().Be(Constants.UserLockedCode);
        _context.Preferences.Values.Should().BeEmpty();
    }

    [Fact]
    public async Task PasswordExpiredAccount_IsDenied()
    {
        _context.AddUser("expired-user", passwordExpired: true);

        var load = await _context.ExecuteAsync(LoadQuery("accountDashboard"), "expired-user");
        var save = await _context.ExecuteAsync(SaveMutation("accountDashboard"), "expired-user");

        GraphQlTestContext.Errors(load).Should().ContainSingle().Which.Code.Should().Be(Constants.PasswordExpiredCode);
        GraphQlTestContext.Errors(save).Should().ContainSingle().Which.Code.Should().Be(Constants.PasswordExpiredCode);
        _context.Preferences.Values.Should().BeEmpty();
    }

    [Fact]
    public void Schema_HasNoUserArgumentOrField()
    {
        // The layout's owner comes from the token only: no argument or input field may name a user.
        _context.Schema.Initialize();

        _context.Schema.Query.GetField("layout").Arguments.Select(x => x.Name).Should().BeEquivalentTo("scope", "storeId");
        _context.Schema.Mutation.GetField("saveLayout").Arguments.Select(x => x.Name).Should().BeEquivalentTo("command");

        var input = (IInputObjectGraphType)_context.Schema.AllTypes["InputLayout"];
        input.Fields.Select(x => x.Name).Should().BeEquivalentTo("scope", "storeId", "schemaVersion", "regions");
    }

    private static string SaveMutation(string scope, string storeId = "B2B-store", int schemaVersion = 1)
    {
        return $$"""
                 mutation {
                   saveLayout(command: {
                     scope: "{{scope}}"
                     storeId: "{{storeId}}"
                     schemaVersion: {{schemaVersion}}
                     regions: [
                       { id: "statistics", blocks: [
                         { id: "orders_placed_mtd", type: "orders_placed_mtd", hidden: false, settings: [] },
                         { id: "avg_order_value", type: "avg_order_value", hidden: true, settings: [] }
                       ] },
                       { id: "mainLeft", blocks: [
                         { id: "recent_orders", type: "recent_orders", hidden: false, settings: [
                           { key: "maxRows", value: 5 },
                           { key: "compact", value: true },
                           { key: "tab.Payment required", value: false }
                         ] }
                       ] }
                     ]
                   }) {
                     schemaVersion
                     modifiedDate
                   }
                 }
                 """;
    }

    private static string LoadQuery(string scope, string storeId = "B2B-store")
    {
        var storeArgument = storeId == null ? string.Empty : $", storeId: \"{storeId}\"";

        return $$"""
                 query {
                   layout(scope: "{{scope}}"{{storeArgument}}) {
                     schemaVersion
                     modifiedDate
                     regions { id blocks { id type hidden settings { key value } } }
                   }
                 }
                 """;
    }

    private static JsonElement SettingValue(JsonElement block, string key)
    {
        return block.GetProperty("settings").EnumerateArray().Single(x => x.GetProperty("key").GetString() == key).GetProperty("value");
    }
}

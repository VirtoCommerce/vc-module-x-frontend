using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XFrontend.Core.Layouts.Commands;

namespace VirtoCommerce.XFrontend.Core.Layouts.Schemas;

public class InputLayoutType : ExtendableInputObjectGraphType<SaveLayoutCommand>
{
    public InputLayoutType()
    {
        Name = "InputLayout";

        Field<NonNullGraphType<StringGraphType>>(nameof(SaveLayoutCommand.Scope)).Description("Dashboard the layout belongs to (\"accountDashboard\").");
        Field<StringGraphType>(nameof(SaveLayoutCommand.StoreId)).Description("Store the layout is saved for.");
        Field<NonNullGraphType<IntGraphType>>(nameof(SaveLayoutCommand.SchemaVersion)).Description("Document schema version.");
        Field<NonNullGraphType<ListGraphType<NonNullGraphType<InputLayoutRegionType>>>>(nameof(SaveLayoutCommand.Regions)).Description("Fixed regions of the dashboard with their blocks; replaces the saved layout as a whole.");
    }
}

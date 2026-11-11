using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XFrontend.Core.Layouts.Models;

namespace VirtoCommerce.XFrontend.Core.Layouts.Schemas;

public class InputLayoutRegionType : ExtendableInputObjectGraphType<LayoutRegion>
{
    public InputLayoutRegionType()
    {
        Name = "InputLayoutRegion";

        Field<NonNullGraphType<StringGraphType>>(nameof(LayoutRegion.Id)).Description("Region id (\"statistics\", \"mainLeft\").");
        Field<NonNullGraphType<ListGraphType<NonNullGraphType<InputLayoutBlockType>>>>(nameof(LayoutRegion.Blocks)).Description("Blocks in render order.");
    }
}

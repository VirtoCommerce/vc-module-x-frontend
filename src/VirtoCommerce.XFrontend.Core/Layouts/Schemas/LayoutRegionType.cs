using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XFrontend.Core.Layouts.Models;

namespace VirtoCommerce.XFrontend.Core.Layouts.Schemas;

public class LayoutRegionType : ExtendableGraphType<LayoutRegion>
{
    public LayoutRegionType()
    {
        Name = "LayoutRegion";

        Field(x => x.Id, nullable: false).Description("Region id (\"statistics\", \"mainLeft\").");
        Field<NonNullGraphType<ListGraphType<NonNullGraphType<LayoutBlockType>>>>(nameof(LayoutRegion.Blocks)).Description("Blocks in render order.");
    }
}

using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XFrontend.Core.Layouts.Models;

namespace VirtoCommerce.XFrontend.Core.Layouts.Schemas;

public class LayoutType : ExtendableGraphType<Layout>
{
    public LayoutType()
    {
        Name = "Layout";

        Field(x => x.SchemaVersion, nullable: false).Description("Document schema version, for the storefront to migrate older saved layouts.");
        Field<NonNullGraphType<ListGraphType<NonNullGraphType<LayoutRegionType>>>>(nameof(Layout.Regions)).Description("Fixed regions of the dashboard.");
        Field(x => x.ModifiedDate, nullable: true).Description("When the layout was last saved (UTC).");
    }
}

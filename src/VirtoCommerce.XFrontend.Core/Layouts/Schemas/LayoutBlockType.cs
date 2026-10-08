using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XFrontend.Core.Layouts.Models;

namespace VirtoCommerce.XFrontend.Core.Layouts.Schemas;

public class LayoutBlockType : ExtendableGraphType<LayoutBlock>
{
    public LayoutBlockType()
    {
        Name = "LayoutBlock";

        Field(x => x.Id, nullable: false).Description("Block id, unique within the layout.");
        Field(x => x.Type, nullable: false).Description("Block type, from the storefront vocabulary.");
        Field(x => x.Hidden, nullable: false).Description("Whether the user hid the block.");
        Field<NonNullGraphType<ListGraphType<NonNullGraphType<LayoutSettingType>>>>(nameof(LayoutBlock.Settings)).Description("Block settings; may be empty.");
    }
}

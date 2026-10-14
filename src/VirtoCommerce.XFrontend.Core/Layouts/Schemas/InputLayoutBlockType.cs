using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XFrontend.Core.Layouts.Models;

namespace VirtoCommerce.XFrontend.Core.Layouts.Schemas;

public class InputLayoutBlockType : ExtendableInputObjectGraphType<LayoutBlock>
{
    public InputLayoutBlockType()
    {
        Name = "InputLayoutBlock";

        Field<NonNullGraphType<StringGraphType>>(nameof(LayoutBlock.Id)).Description("Block id, unique within the layout.");
        Field<NonNullGraphType<StringGraphType>>(nameof(LayoutBlock.Type)).Description("Block type, from the storefront vocabulary.");
        Field<NonNullGraphType<BooleanGraphType>>(nameof(LayoutBlock.Hidden)).Description("Whether the user hid the block.");
        Field<NonNullGraphType<ListGraphType<NonNullGraphType<InputLayoutSettingType>>>>(nameof(LayoutBlock.Settings)).Description("Block settings; send an empty list for none.");
    }
}

using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.Xapi.Core.Schemas.ScalarTypes;
using VirtoCommerce.XFrontend.Core.Layouts.Models;

namespace VirtoCommerce.XFrontend.Core.Layouts.Schemas;

public class InputLayoutSettingType : ExtendableInputObjectGraphType<LayoutSetting>
{
    public InputLayoutSettingType()
    {
        Name = "InputLayoutSetting";

        Field<NonNullGraphType<StringGraphType>>(nameof(LayoutSetting.Key)).Description("Setting key, from the block type vocabulary.");
        Field<AnyValueGraphType>(nameof(LayoutSetting.Value)).Description("Scalar setting value: string, number or boolean.");
    }
}

using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.Xapi.Core.Schemas.ScalarTypes;
using VirtoCommerce.XFrontend.Core.Layouts.Models;

namespace VirtoCommerce.XFrontend.Core.Layouts.Schemas;

public class LayoutSettingType : ExtendableGraphType<LayoutSetting>
{
    public LayoutSettingType()
    {
        Name = "LayoutSetting";

        Field(x => x.Key, nullable: false).Description("Setting key, from the block type vocabulary.");
        Field<AnyValueGraphType>(nameof(LayoutSetting.Value)).Description("Scalar setting value: string, number or boolean.");
    }
}

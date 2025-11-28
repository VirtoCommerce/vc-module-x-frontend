using VirtoCommerce.ProfileExperienceApiModule.Data.Schemas;
using VirtoCommerce.WhiteLabeling.ExperienceApi.Schemas;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XFrontend.Core.Models;

namespace VirtoCommerce.XFrontend.Core.Schemas;

public class PageContextResponseType : ExtendableGraphType<PageContextResponse>
{
    public PageContextResponseType()
    {
        Field<SlugInfoResponseType>("slugInfo").Resolve(context => context.Source.SlugInfoResponse);
        Field<StoreResponseType>("store").Resolve(context => context.Source.StoreResponse);
        Field<WhiteLabelingSettingsType>("whiteLabelingSettings").Resolve(context => context.Source.WhiteLabelingSetting);
        Field<UserType>("user").Resolve(context => context.Source.User);
    }
}

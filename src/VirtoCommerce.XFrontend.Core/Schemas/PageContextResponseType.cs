using VirtoCommerce.WhiteLabeling.ExperienceApi.Schemas;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XFrontend.Core.Models;
using UserType = VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.UserType;

namespace VirtoCommerce.XFrontend.Core.Schemas;

public class PageContextResponseType : ExtendableGraphType<PageContextResponse>
{
    public PageContextResponseType()
    {
        Field<SlugInfoResponseType>("slugInfo").Resolve(context => context.Source.SlugInfoResponse);
        Field<StoreResponseType>("store").Resolve(context => context.Source.StoreResponse);
        Field<WhiteLabelingSettingsType>("whiteLabelingSettings").Resolve(context => context.Source.WhiteLabelingSetting);
        ExtendableField<UserType>(name: "user", description: "User info", resolve: context => context.Source.User);
    }
}

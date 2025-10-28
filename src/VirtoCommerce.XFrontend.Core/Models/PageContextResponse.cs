using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.WhiteLabeling.ExperienceApi.Models;
using VirtoCommerce.Xapi.Core.Models;

namespace VirtoCommerce.XFrontend.Core.Models;

public class PageContextResponse
{
    public ExpWhiteLabelingSetting WhiteLabelingSetting { get; set; }

    public StoreResponse StoreResponse { get; set; }

    public SlugInfoResponse SlugInfoResponse { get; set; }

    public ApplicationUser User { get; set; }
}

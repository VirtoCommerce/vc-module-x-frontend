using System;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.WhiteLabeling.ExperienceApi.Models;
using VirtoCommerce.Xapi.Core.Models;

namespace VirtoCommerce.XFrontend.Core.Models;

public class PageContextResponse
{
    [Obsolete("Always null. White labeling initialization moved to White labeling module.", DiagnosticId = "VC0012", UrlFormat = "https://docs.virtocommerce.org/platform/user-guide/versions/virto3-products-versions/")]
    public ExpWhiteLabelingSetting WhiteLabelingSetting { get; set; }

    public string CultureName { get; set; }

    public string OrganizationId { get; set; }

    public StoreResponse StoreResponse { get; set; }

    public SlugInfoResponse SlugInfoResponse { get; set; }

    public ApplicationUser User { get; set; }
}

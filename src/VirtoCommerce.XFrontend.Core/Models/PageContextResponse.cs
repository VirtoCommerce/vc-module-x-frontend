using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Models;

namespace VirtoCommerce.XFrontend.Core.Models;

public class PageContextResponse
{
    public string CultureName { get; set; }

    public string OrganizationId { get; set; }

    public StoreResponse StoreResponse { get; set; }

    public SlugInfoResponse SlugInfoResponse { get; set; }

    public ApplicationUser User { get; set; }
}

using System.Collections.Generic;

namespace VirtoCommerce.XFrontend.Core.Layouts.Models;

public class LayoutRegion
{
    public string Id { get; set; }

    public IList<LayoutBlock> Blocks { get; set; } = [];
}

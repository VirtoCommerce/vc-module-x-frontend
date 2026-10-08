using System;
using System.Collections.Generic;

namespace VirtoCommerce.XFrontend.Core.Layouts.Models;

public class Layout
{
    public int SchemaVersion { get; set; }

    public IList<LayoutRegion> Regions { get; set; } = [];

    public DateTime? ModifiedDate { get; set; }
}

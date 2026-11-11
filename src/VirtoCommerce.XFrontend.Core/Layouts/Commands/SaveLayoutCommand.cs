using System.Collections.Generic;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XFrontend.Core.Layouts.Models;

namespace VirtoCommerce.XFrontend.Core.Layouts.Commands;

public class SaveLayoutCommand : ICommand<Layout>
{
    public string Scope { get; set; }

    public string StoreId { get; set; }

    public int SchemaVersion { get; set; }

    public IList<LayoutRegion> Regions { get; set; } = [];

    public string UserId { get; set; }
}

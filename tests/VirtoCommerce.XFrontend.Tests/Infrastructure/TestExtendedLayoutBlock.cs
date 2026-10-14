using VirtoCommerce.XFrontend.Core.Layouts.Models;

namespace VirtoCommerce.XFrontend.Tests.Infrastructure;

/// <summary>
/// Stands in for a downstream module's derived block type: extends <see cref="LayoutBlock"/> with a field the base
/// contract knows nothing about. Registered in <see cref="TestAssemblyInitializer"/>, so the layout round-trip
/// proves a derived element of a nested collection (and its extra field) survives persistence.
/// </summary>
public class TestExtendedLayoutBlock : LayoutBlock
{
    public string ColorScheme { get; set; }
}

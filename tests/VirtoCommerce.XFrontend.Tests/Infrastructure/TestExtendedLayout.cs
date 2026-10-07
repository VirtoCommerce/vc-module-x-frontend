using VirtoCommerce.XFrontend.Core.Layouts.Models;

namespace VirtoCommerce.XFrontend.Tests.Infrastructure;

/// <summary>
/// Stands in for a downstream module's derived ROOT layout type: extends <see cref="Layout"/> with a field the base
/// contract knows nothing about. Proves the root deserialization (<c>DeserializeObject&lt;Layout&gt;</c>) returns the
/// registered derived type, not the base generic argument.
/// </summary>
public class TestExtendedLayout : Layout
{
    public string Theme { get; set; }
}

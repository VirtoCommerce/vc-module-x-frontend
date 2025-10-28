using VirtoCommerce.Xapi.Core.Models;

namespace VirtoCommerce.XFrontend.Core;

/// <summary>
/// Anchor class for easy DI GraphQL schemas
/// </summary>
[OptionalGraphQlTypesContainer(DependencyName = "VirtoCommerce.WhiteLabeling.ExperienceApi")]
public class CoreAssemblyMarker
{
}

using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading;
using GraphQL;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.XFrontend.Core.Layouts.Models;

namespace VirtoCommerce.XFrontend.Tests.Infrastructure;

/// <summary>
/// Process-wide setup, done ONCE at assembly load via a ModuleInitializer: AbstractTypeFactory and the platform switches
/// are static, AbstractTypeFactory is not thread-safe and xunit runs test classes in parallel, so none of this may
/// happen per test.
/// </summary>
internal static class TestAssemblyInitializer
{
    private static int _initialized;

    [ModuleInitializer]
    internal static void Initialize()
    {
        if (Interlocked.Exchange(ref _initialized, 1) != 0)
        {
            return;
        }

        // The platform reads the current user id from these claim types; platform startup sets them.
        ClaimsPrincipalExtensions.UserIdClaimTypes = [ClaimTypes.NameIdentifier];

        // What the Xapi module sets at startup: legacy naming keeps the "Type" suffix of unnamed graph types (MoneyType).
#pragma warning disable CS0618 // Type or member is obsolete - mirrors the Xapi module, which sets the same switches.
        GlobalSwitches.UseLegacyTypeNaming = true;
        GlobalSwitches.InferFieldNullabilityFromNRTAnnotations = false;
#pragma warning restore CS0618

        // Simulates a downstream module extending the layout contract with derived types, at the ROOT (Layout) and
        // in a nested collection (LayoutBlock), so the persistence round-trip proves both survive.
        AbstractTypeFactory<Layout>.OverrideType<Layout, TestExtendedLayout>();
        AbstractTypeFactory<LayoutBlock>.OverrideType<LayoutBlock, TestExtendedLayoutBlock>();
    }
}

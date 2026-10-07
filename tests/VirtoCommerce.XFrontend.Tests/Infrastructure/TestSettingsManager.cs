using Moq;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.XFrontend.Core;

namespace VirtoCommerce.XFrontend.Tests.Infrastructure;

/// <summary>A settings manager that answers only the order statistics cache lifetime, with the given value.</summary>
public static class TestSettingsManager
{
    public static ISettingsManager WithOrderCacheExpiration(int minutes)
    {
        var settingsManager = new Mock<ISettingsManager>();
        settingsManager
            .Setup(x => x.GetObjectSettingAsync(ModuleConstants.Settings.Statistics.OrderCacheExpirationMinutes.Name, null, null))
            .ReturnsAsync(new ObjectSettingEntry { Value = minutes });

        return settingsManager.Object;
    }
}

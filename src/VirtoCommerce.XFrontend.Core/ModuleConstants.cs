using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.XFrontend.Core;

public static class ModuleConstants
{
    public static class Layouts
    {
        public const string ScopePattern = @"^[A-Za-z][A-Za-z0-9]{0,63}\z";
        public const string StoreIdPattern = @"^[A-Za-z0-9_-]{1,128}\z";
        public const string IdPattern = @"^[A-Za-z0-9_-]{1,64}\z";
        public const int MaxRegions = 20;
        public const int MaxBlocksPerRegion = 50;
        public const int MaxSettingsPerBlock = 50;
        public const int MaxSettingKeyLength = 64;
        public const int MaxSettingValueSize = 4 * 1024;
        public const int MaxLayoutSize = 64 * 1024;
        public const int MaxLayoutsPerUser = 20;
    }

    public static class Settings
    {
        /// <summary>
        /// Store brand identity, exposed to storefronts and AI agents as schema.org Organization / OnlineStore data.
        /// </summary>
        public static class BrandProfile
        {
            public const string GroupName = "Virto Commerce Frontend|Store Information";

            public static SettingDescriptor Description { get; } = new SettingDescriptor
            {
                Name = "XFrontend.BrandProfile.Description",
                ValueType = SettingValueType.LongText,
                GroupName = GroupName,
                DefaultValue = string.Empty,
                IsPublic = true
            };

            public static SettingDescriptor SameAs { get; } = new SettingDescriptor
            {
                Name = "XFrontend.BrandProfile.SameAs",
                ValueType = SettingValueType.LongText,
                GroupName = GroupName,
                DefaultValue = string.Empty,
                IsPublic = true
            };

            public static SettingDescriptor Tagline { get; } = new SettingDescriptor
            {
                Name = "XFrontend.BrandProfile.Tagline",
                ValueType = SettingValueType.ShortText,
                GroupName = GroupName,
                DefaultValue = string.Empty,
                IsPublic = true
            };

            public static SettingDescriptor LogoUrl { get; } = new SettingDescriptor
            {
                Name = "XFrontend.BrandProfile.LogoUrl",
                ValueType = SettingValueType.ShortText,
                GroupName = GroupName,
                DefaultValue = string.Empty,
                IsPublic = true
            };

            public static SettingDescriptor ShareImageUrl { get; } = new SettingDescriptor
            {
                Name = "XFrontend.BrandProfile.ShareImageUrl",
                ValueType = SettingValueType.ShortText,
                GroupName = GroupName,
                DefaultValue = string.Empty,
                IsPublic = true
            };

            public static SettingDescriptor ContactPhone { get; } = new SettingDescriptor
            {
                Name = "XFrontend.BrandProfile.ContactPhone",
                ValueType = SettingValueType.ShortText,
                GroupName = GroupName,
                DefaultValue = string.Empty,
                IsPublic = true
            };

            public static SettingDescriptor FoundingDate { get; } = new SettingDescriptor
            {
                Name = "XFrontend.BrandProfile.FoundingDate",
                ValueType = SettingValueType.ShortText,
                GroupName = GroupName,
                DefaultValue = string.Empty,
                IsPublic = true
            };

            public static IEnumerable<SettingDescriptor> AllSettings
            {
                get
                {
                    yield return Description;
                    yield return SameAs;
                    yield return Tagline;
                    yield return LogoUrl;
                    yield return ShareImageUrl;
                    yield return ContactPhone;
                    yield return FoundingDate;
                }
            }
        }

        public static class Statistics
        {
            public const string GroupName = "Virto Commerce Frontend|Statistics";

            public static SettingDescriptor OrderCacheExpirationMinutes { get; } = new SettingDescriptor
            {
                Name = "XFrontend.Statistics.Order.CacheExpirationMinutes",
                ValueType = SettingValueType.Integer,
                GroupName = GroupName,
                DefaultValue = 5
            };

            public static IEnumerable<SettingDescriptor> AllSettings
            {
                get
                {
                    yield return OrderCacheExpirationMinutes;
                }
            }
        }

        public static IEnumerable<SettingDescriptor> AllSettings => BrandProfile.AllSettings.Concat(Statistics.AllSettings);

        public static IEnumerable<SettingDescriptor> StoreLevelSettings => BrandProfile.AllSettings;
    }
}

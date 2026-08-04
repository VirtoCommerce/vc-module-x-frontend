using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.XFrontend.Core;

public static class ModuleConstants
{
    public static class Settings
    {
        /// <summary>
        /// Store brand identity, exposed to storefronts and AI agents as schema.org Organization / OnlineStore data.
        /// </summary>
        public static class BrandProfile
        {
            public const string GroupName = "Virto Commerce Frontend|Store Information";

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
                    yield return SameAs;
                    yield return Tagline;
                    yield return LogoUrl;
                    yield return ShareImageUrl;
                    yield return ContactPhone;
                    yield return FoundingDate;
                }
            }
        }

        public static IEnumerable<SettingDescriptor> AllSettings => BrandProfile.AllSettings;

        public static IEnumerable<SettingDescriptor> StoreLevelSettings => BrandProfile.AllSettings;
    }
}

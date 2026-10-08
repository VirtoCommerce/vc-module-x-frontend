using System.Linq;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XFrontend.Core;
using VirtoCommerce.XFrontend.Core.Layouts.Commands;
using VirtoCommerce.XFrontend.Core.Layouts.Services;
using VirtoCommerce.XFrontend.Core.Statistics.Services;
using VirtoCommerce.XFrontend.Data.Layouts.Commands;
using VirtoCommerce.XFrontend.Data.Layouts.Queries;
using VirtoCommerce.XFrontend.Data.Layouts.Services;
using VirtoCommerce.XFrontend.Data.Layouts.Validation;
using VirtoCommerce.XFrontend.Data.Statistics.Queries;
using VirtoCommerce.XFrontend.Data.Statistics.Services;
using VirtoCommerce.XFrontend.Web;
using Xunit;

namespace VirtoCommerce.XFrontend.Tests;

/// <summary>Each feature reaches the platform through one registration line; nothing else would catch its loss.</summary>
[Trait("Category", "Unit")]
public class ModuleRegistrationTests
{
    [Fact]
    public void Initialize_RegistersTheServices()
    {
        var services = new ServiceCollection();

        new Module().Initialize(services);

        services.Should().ContainSingle(x => x.ServiceType == typeof(IOrderStatisticsService) && x.ImplementationType == typeof(OrderStatisticsService));
        services.Should().ContainSingle(x => x.ServiceType == typeof(IStatisticsCurrencyResolver) && x.ImplementationType == typeof(StatisticsCurrencyResolver));
        services.Should().ContainSingle(x => x.ServiceType == typeof(ILayoutService) && x.ImplementationType == typeof(LayoutService));
        services.Should().ContainSingle(x => x.ServiceType == typeof(AbstractValidator<SaveLayoutCommand>) && x.ImplementationType == typeof(SaveLayoutCommandValidator));
    }

    [Fact]
    public void Initialize_RegistersTheSchemaBuilders()
    {
        var services = new ServiceCollection();

        new Module().Initialize(services);

        services.Where(x => x.ServiceType == typeof(ISchemaBuilder)).Select(x => x.ImplementationType)
            .Should().Contain(typeof(OrderStatisticsQueryBuilder))
            .And.Contain(typeof(LayoutQueryBuilder))
            .And.Contain(typeof(SaveLayoutCommandBuilder));
    }

    [Fact]
    public void Settings_IncludeTheOrderStatisticsCacheLifetime_AsAGlobalSetting()
    {
        var setting = ModuleConstants.Settings.AllSettings.Should()
            .ContainSingle(x => x.Name == "XFrontend.Statistics.Order.CacheExpirationMinutes").Subject;

        setting.ValueType.Should().Be(SettingValueType.Integer);
        setting.DefaultValue.Should().Be(5);
        ModuleConstants.Settings.StoreLevelSettings.Should().NotContain(setting);
    }
}

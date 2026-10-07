using FluentValidation;
using GraphQL.MicrosoftDI;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XFrontend.Core;
using VirtoCommerce.XFrontend.Core.Layouts.Commands;
using VirtoCommerce.XFrontend.Core.Layouts.Services;
using VirtoCommerce.XFrontend.Core.Statistics.Services;
using VirtoCommerce.XFrontend.Data;
using VirtoCommerce.XFrontend.Data.Layouts.Services;
using VirtoCommerce.XFrontend.Data.Layouts.Validation;
using VirtoCommerce.XFrontend.Data.Statistics.Services;

namespace VirtoCommerce.XFrontend.Web;

public class Module : IModule, IHasConfiguration
{
    public ManifestModuleInfo ModuleInfo { get; set; }
    public IConfiguration Configuration { get; set; }

    public void Initialize(IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IOrderStatisticsService, OrderStatisticsService>();
        serviceCollection.AddTransient<IStatisticsCurrencyResolver, StatisticsCurrencyResolver>();
        serviceCollection.AddTransient<ILayoutService, LayoutService>();
        serviceCollection.AddTransient<AbstractValidator<SaveLayoutCommand>, SaveLayoutCommandValidator>();

        // Register GraphQL schema
        _ = new GraphQLBuilder(serviceCollection, builder =>
        {
            builder.AddSchema(serviceCollection, typeof(CoreAssemblyMarker), typeof(DataAssemblyMarker));
        });
    }

    public void PostInitialize(IApplicationBuilder appBuilder)
    {
        var serviceProvider = appBuilder.ApplicationServices;

        // Register settings
        var settingsRegistrar = serviceProvider.GetRequiredService<ISettingsRegistrar>();
        settingsRegistrar.RegisterSettings(ModuleConstants.Settings.AllSettings, ModuleInfo.Id);
        settingsRegistrar.RegisterSettingsForType(ModuleConstants.Settings.StoreLevelSettings, nameof(Store));
    }

    public void Uninstall()
    {
        // Nothing to do here
    }
}

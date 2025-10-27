using GraphQL.MicrosoftDI;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XFrontend.Core;
using VirtoCommerce.XFrontend.Data;

namespace VirtoCommerce.XFrontend.Web;

public class Module : IModule, IHasConfiguration
{
    public ManifestModuleInfo ModuleInfo { get; set; }
    public IConfiguration Configuration { get; set; }

    public void Initialize(IServiceCollection serviceCollection)
    {
        // Register services
        //serviceCollection.AddTransient<IMyService, MyService>();

        // Register GraphQL schema
        var graphQlBuilder = new GraphQLBuilder(serviceCollection, builder =>
        {
            builder.AddSchema(serviceCollection, typeof(CoreAssemblyMarker), typeof(DataAssemblyMarker));
        });

        serviceCollection.AddSingleton<ScopedSchemaFactory<DataAssemblyMarker>>();
    }

    public void PostInitialize(IApplicationBuilder appBuilder)
    {
        // Register partial GraphQL schema
        appBuilder.UseScopedSchema<DataAssemblyMarker>("frontend");
    }

    public void Uninstall()
    {
        // Nothing to do here
    }
}

using Microsoft.Extensions.DependencyInjection;
using Soenneker.GraphQL.Generator.Registrars;
using Soenneker.GraphQl.Schema.Conversion.Registrars;
using Soenneker.GraphQl.Schema.Download.Registrars;
using Soenneker.Managers.Runners.Registrars;
using Soenneker.Utils.File.Download.Registrars;
using Soenneker.Railway.Runners.GraphQlClient.Utils;
using Soenneker.Railway.Runners.GraphQlClient.Utils.Abstract;

namespace Soenneker.Railway.Runners.GraphQlClient;

/// <summary>
/// Console type startup
/// </summary>
public static class Startup
{
    // This method gets called by the runtime. Use this method to add services to the container.
    public static void ConfigureServices(IServiceCollection services)
    {
        services.SetupIoC();
    }

    public static IServiceCollection SetupIoC(this IServiceCollection services)
    {
        services.AddHostedService<ConsoleHostedService>()
                .AddSingleton<IFileOperationsUtil, FileOperationsUtil>()
                .AddRunnersManagerAsSingleton()
                .AddFileDownloadUtilAsSingleton()
                .AddGraphQlGeneratorAsSingleton()
                .AddGraphQlSchemaDownloadUtilAsSingleton()
                .AddGraphQlSchemaConversionUtilAsSingleton();

        return services;
    }
}

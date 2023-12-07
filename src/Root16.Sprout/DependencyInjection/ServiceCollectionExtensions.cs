using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Root16.Sprout.BatchProcessing;
using Root16.Sprout.DataSources.Dataverse;
using Root16.Sprout.Progress;

namespace Root16.Sprout.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStep<TStep>(this IServiceCollection services)
        where TStep : class, IIntegrationStep
    {
        services.AddSingleton(new StepRegistration(typeof(TStep)));
        services.AddTransient<TStep>();

        return services;
    }

    public static IServiceCollection AddStep<TStep>(this IServiceCollection services, string name)
        where TStep : class, IIntegrationStep
    {
        services.AddSingleton(new StepRegistration(typeof(TStep), name));
        services.AddTransient<TStep>();

        return services;
    }

    public static IServiceCollection AddDataverseDataSource(this IServiceCollection services, string connectionStringName)
    {
        return services.AddTransient(services =>
        {
            var factory = services.GetRequiredService<IDataverseDataSourceFactory>();
            return factory.CreateDataSource(connectionStringName);
        });
    }

    public static IServiceCollection AddSprout(this IServiceCollection services)
    {
        services.TryAddSingleton<IIntegrationRuntime, IntegrationRuntime>();
        services.TryAddTransient<BatchRunner>();
        services.TryAddSingleton<IProgressListener, ConsoleProgressListener>();
        services.TryAddTransient<DataverseDataSource>();
        services.TryAddTransient<EntityReducer>();
        services.TryAddSingleton<IDataverseDataSourceFactory, DataverseDataSourceFactory>();
        return services;
    }
}

public interface IDataverseDataSourceFactory
{
    DataverseDataSource CreateDataSource(string name);
}

public class DataverseDataSourceFactory : IDataverseDataSourceFactory
{
    private readonly IServiceProvider serviceProvider;

    public DataverseDataSourceFactory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public DataverseDataSource CreateDataSource(string connectionStringName)
    {
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        var logger = serviceProvider.GetRequiredService<ILogger<DataverseDataSource>>();
        var serviceClient = new ServiceClient(
            config.GetConnectionString(connectionStringName),
            serviceProvider.GetRequiredService<ILogger<ServiceClient>>()
        );
        var ds = new DataverseDataSource(serviceClient, logger);
        return ds;
    }
}

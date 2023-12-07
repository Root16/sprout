using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

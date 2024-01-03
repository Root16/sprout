using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Root16.Sprout.BatchProcessing;
using Root16.Sprout.DataSources.Dataverse;
using Root16.Sprout.Progress;

namespace Root16.Sprout.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterStep<TStep>(this IServiceCollection services, List<string>? dependentSteps = null) where TStep : class, IIntegrationStep
    {
        services.AddSingleton(new StepRegistration(typeof(TStep), dependentSteps));
        services.AddTransient<TStep>();

        return services;
    }

    public static IServiceCollection AddSprout(this IServiceCollection services)
    {
        services.TryAddSingleton<IIntegrationRuntime, IntegrationRuntime>();
        services.TryAddTransient<BatchProcessor>();
        services.TryAddSingleton<IProgressListener, ConsoleProgressListener>();
        services.TryAddTransient<DataverseDataSource>();
        services.TryAddTransient<EntityOperationReducer>();
        services.TryAddSingleton<IDataverseDataSourceFactory, DataverseDataSourceFactory>();
        return services;
    }
}

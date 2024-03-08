using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Root16.Sprout.BatchProcessing;
using Root16.Sprout.DependencyInjection;
using Root16.Sprout.Progress;

namespace Root16.Sprout;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterStep<TStep>(this IServiceCollection services, params string[] prerequisiteStepNames) where TStep : class, IIntegrationStep
    {
        services.AddSingleton(new StepRegistration(typeof(TStep), prerequisiteStepNames.ToList()));
        services.AddTransient<TStep>();

        return services;
    }

    public static IServiceCollection AddSprout(this IServiceCollection services)
    {
        services.TryAddSingleton<IIntegrationRuntime, IntegrationRuntime>();
        services.TryAddTransient<IBatchProcessor, BatchProcessor>();
        services.TryAddSingleton<IProgressListener, ConsoleProgressListener>();
        return services;
    }
}

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
        services.AddSingleton(new StepRegistration(typeof(TStep), [.. prerequisiteStepNames]));
        services.AddKeyedTransient<TStep>(typeof(TStep).Name);

        return services;
    }

    public static IServiceCollection RegisterStep<TStep>(this IServiceCollection services, Func<IServiceProvider, object?, TStep> keyedImplementationFactory, params string[] prerequisiteStepNames) where TStep : class, IIntegrationStep
    {
        services.AddSingleton(new StepRegistration(typeof(TStep), [.. prerequisiteStepNames]));
        services.AddKeyedTransient<TStep>(typeof(TStep).Name, (serviceProvider, myKey) =>
        {
            return keyedImplementationFactory(serviceProvider, myKey);
        });

        return services;
    }

    public static IServiceCollection RegisterStep<TStep>(this IServiceCollection services, string StepName, Func<IServiceProvider, object?, TStep> keyedImplementationFactory, params string[] prerequisiteStepNames) where TStep : class, IIntegrationStep
    {
        services.AddSingleton(new StepRegistration(typeof(TStep), StepName, [.. prerequisiteStepNames]));
        services.AddKeyedTransient<TStep>(StepName, (serviceProvider, myKey) =>
        {
            return keyedImplementationFactory(serviceProvider, myKey);
        });

        return services;
    }

    public static IServiceCollection AddSprout(this IServiceCollection services)
    {
        services.TryAddSingleton<IIntegrationRuntime, IntegrationRuntime>();
        services.TryAddTransient<BatchProcessor>();
        services.TryAddSingleton<IProgressListener, ConsoleProgressListener>();
        return services;
    }

    public static IServiceCollection RemoveConsoleListener(this IServiceCollection services)
    {
        var progressListenerToRemove = services.FirstOrDefault(x => x.ServiceType.Name == typeof(IProgressListener).Name);
        if (progressListenerToRemove is not null)
        {
            services.Remove(progressListenerToRemove);
        }
        return services;
    }

    public static IServiceCollection AddSproutWithBatchDelay(this IServiceCollection services, TimeSpan defaultBatchDelay)
    {
        services.TryAddSingleton<IIntegrationRuntime, IntegrationRuntime>();
        services.TryAddTransient<BatchProcessor>((serviceProvider) =>
        {
            return new (serviceProvider.GetRequiredService<IProgressListener>(), defaultBatchDelay);
        });
        services.TryAddSingleton<IProgressListener, ConsoleProgressListener>();
        return services;
    }
}

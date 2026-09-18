using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Root16.Sprout.BatchProcessing;
using Root16.Sprout.DependencyInjection;
using Root16.Sprout.Logging;
using Root16.Sprout.Progress;

namespace Root16.Sprout;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// This method registers a step with the service collection, allowing you to specify prerequisite steps that should run before the step being registered is run.
    /// </summary>
    /// <typeparam name="TStep">The Type of the step that should be created.</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="prerequisiteStepNames">The names of the steps that should run before the step being registered is run</param>
    /// <returns>Returns the service collection</returns>
    public static IServiceCollection RegisterStep<TStep>(this IServiceCollection services, params IEnumerable<string> prerequisiteStepNames) where TStep : class, IIntegrationStep
    {
        services.AddSingleton(new StepRegistration(typeof(TStep), [.. prerequisiteStepNames]));
        services.AddKeyedTransient<TStep>(typeof(TStep).Name);

        return services;
    }

    /// <summary>
    /// This method registers a step with the service collection, allowing you to specify prerequisite steps that should run before the step being registered is run, and also allows you to specify custom arguments that will be passed to the step's constructor when it is created.
    /// Custom arguments are useful for passing configuration or other data to the step when it is created
    /// Custom arguments are passed to the step's constructor in the order they are provided, so make sure to provide them in the correct order for the step's constructor.
    /// Custom arguments are not registered with the service collection, so they will not be available for dependency injection in other steps and they should not be registered with the service collection. They are only used for the step being registered.
    /// </summary>
    /// <typeparam name="TStep">The Type of the step that should be created.</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="customArguments">This m</param>
    /// <param name="prerequisiteStepNames">The names of the steps that should run before the step being registered is run</param>
    /// <returns>Returns the service collection</returns>
    public static IServiceCollection RegisterStepWithArguments<TStep>(this IServiceCollection services, IEnumerable<object> customArguments, params IEnumerable<string> prerequisiteStepNames) where TStep : class, IIntegrationStep
    {
        services.AddSingleton(new StepRegistration(typeof(TStep), [.. prerequisiteStepNames]));
        services.AddKeyedTransient<TStep>(typeof(TStep).Name, (serviceProvider, myKey) =>
        {
            return ActivatorUtilities.CreateInstance<TStep>(serviceProvider, [.. customArguments]);
        });

        return services;
    }

    /// <summary>
    /// This method registers a step with the service collection, allowing you to specify a custom implementation factory for the step.
    /// </summary>
    /// <typeparam name="TStep">The Type of the step that should be created.</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="keyedImplementationFactory">The factory that will be used to create the step when needed.</param>
    /// <param name="prerequisiteStepNames">The names of the steps that should run before the step being registered is run</param>
    /// <returns>Returns the service collection</returns>
    public static IServiceCollection RegisterStep<TStep>(this IServiceCollection services, Func<IServiceProvider, object?, TStep> keyedImplementationFactory, params IEnumerable<string> prerequisiteStepNames) where TStep : class, IIntegrationStep
    {
        services.AddSingleton(new StepRegistration(typeof(TStep), [.. prerequisiteStepNames]));
        services.AddKeyedTransient<TStep>(typeof(TStep).Name, (serviceProvider, myKey) =>
        {
            return keyedImplementationFactory(serviceProvider, myKey);
        });

        return services;
    }

    /// <summary>
    /// This method registers a step with the service collection, allowing you to specify prerequisite steps that should run before the step being registered is run, a custom step name, and also allows you to specify custom arguments that will be passed to the step's constructor when it is created.
    /// Custom arguments are useful for passing configuration or other data to the step when it is created
    /// Custom arguments are passed to the step's constructor in the order they are provided, so make sure to provide them in the correct order for the step's constructor.
    /// Custom arguments are not registered with the service collection, so they will not be available for dependency injection in other steps and they should not be registered with the service collection. They are only used for the step being registered.
    /// </summary>
    /// <typeparam name="TStep">The Type of the step that should be created.</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="stepName">The name you would like to have the step called. Good for when reusing a step with different configuration.</param>
    /// <param name="customArguments"></param>
    /// <param name="prerequisiteStepNames">The names of the steps that should run before the step being registered is run</param>
    /// <returns>Returns the service collection</returns>
    public static IServiceCollection RegisterStepWithArguments<TStep>(this IServiceCollection services, string stepName, IEnumerable<object> customArguments, params IEnumerable<string> prerequisiteStepNames) where TStep : class, IIntegrationStep
    {
        services.AddSingleton(new StepRegistration(typeof(TStep), stepName, [.. prerequisiteStepNames]));
        services.AddKeyedTransient<TStep>(stepName, (serviceProvider, myKey) =>
        {
            return ActivatorUtilities.CreateInstance<TStep>(serviceProvider, [.. customArguments]);
        });

        return services;
    }

    /// <summary>
    /// This method registers a step with the service collection, allowing you to specify a custom implementation factory for the step and a custom name for the step.
    /// </summary>
    /// <typeparam name="TStep">The Type of the step that should be created.</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="StepName">The name you would like to have the step called. Good for when reusing a step with different configuration.</param>
    /// <param name="keyedImplementationFactory">The factory that will be used to create the step when needed.</param>
    /// <param name="prerequisiteStepNames">The names of the steps that should run before the step being registered is run</param>
    /// <returns>Returns the service collection</returns>
    public static IServiceCollection RegisterStep<TStep>(this IServiceCollection services, string StepName, Func<IServiceProvider, object?, TStep> keyedImplementationFactory, params IEnumerable<string> prerequisiteStepNames) where TStep : class, IIntegrationStep
    {
        services.AddSingleton(new StepRegistration(typeof(TStep), StepName, [.. prerequisiteStepNames]));
        services.AddKeyedTransient<TStep>(StepName, (serviceProvider, myKey) =>
        {
            return keyedImplementationFactory(serviceProvider, myKey);
        });

        return services;
    }

    /// <summary>
    /// This method registers a step with the service collection, allowing you to specify a configuration object for the step.
    /// </summary>
    /// <typeparam name="TStep">The Type of the step that should be created.</typeparam>
    /// <typeparam name="TConfig">The type of the configuration record</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="stepName">The name you would like to have the step called. Good for when reusing a step with different configuration.</param>
    /// <param name="config">The configuration to be passed to the step</param>
    /// <param name="prerequisiteStepNames">The names of the steps that should run before the step being registered is run</param>
    /// <returns>Returns the service collection</returns>
    public static IServiceCollection RegisterStepWithArguments<TStep, TConfig>(this IServiceCollection services, string stepName, TConfig config, params IEnumerable<string> prerequisiteStepNames) where TStep : class, IIntegrationStep
    {
        services.AddSingleton(new StepRegistration(typeof(TStep), stepName, [.. prerequisiteStepNames]));
        services.AddKeyedTransient<TStep>(stepName, (serviceProvider, myKey) =>
        {
            return ActivatorUtilities.CreateInstance<TStep>(serviceProvider, [config!]);
        });

        return services;
    }

    /// <summary>
    /// This method adds the Sprout services to the service collection, including IIntegrationRuntime, BatchLogger, BatchProcessor, and IProgressListener.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>Returns the service collection</returns>
    public static IServiceCollection AddSprout(this IServiceCollection services)
    {
        services.TryAddSingleton<IIntegrationRuntime, IntegrationRuntime>();
        services.TryAddTransient<BatchProcessor>();
        services.TryAddTransient<BatchLogger>();
        services.TryAddSingleton<IProgressListener, ConsoleProgressListener>();
        return services;
    }

    /// <summary>
    /// This method removes the IProgressListener service from the service collection, effectively disabling console progress listening.
    /// It searches for the registered IProgressListener service and removes it if found.
    /// Should be used when a console is not available or when you want to suppress console output for progress updates.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>Returns the service collection</returns>
    public static IServiceCollection RemoveConsoleListener(this IServiceCollection services)
    {
        var progressListenerToRemove = services.FirstOrDefault(x => x.ServiceType.Name == typeof(IProgressListener).Name);
        if (progressListenerToRemove is not null)
        {
            services.Remove(progressListenerToRemove);
        }
        return services;
    }

    /// <summary>
    /// This method adds the Sprout services to the service collection with a specified default batch delay for the BatchProcessor.
    /// It registers the necessary services including IIntegrationRuntime, BatchLogger, BatchProcessor, and IProgressListener.
    /// The defaultBatchDelay parameter allows you to specify the delay between batch processing operations.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="defaultBatchDelay">The delay that all steps will use to rest between each batch. Can be overridden in specific steps</param>
    /// <returns>Returns the service collection</returns>
    public static IServiceCollection AddSproutWithBatchDelay(this IServiceCollection services, TimeSpan defaultBatchDelay)
    {
        services.TryAddSingleton<IIntegrationRuntime, IntegrationRuntime>();
        services.TryAddTransient<BatchLogger>();
        services.TryAddTransient<BatchProcessor>((serviceProvider) =>
        {
            var batchLogger = serviceProvider.GetRequiredService<BatchLogger>();
            return new (serviceProvider.GetRequiredService<IProgressListener>(), batchLogger, defaultBatchDelay);
        });
        services.TryAddSingleton<IProgressListener, ConsoleProgressListener>();
        return services;
    }
}

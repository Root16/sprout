using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Root16.Sprout.Progress;

namespace Root16.Sprout.SpectreConsole;

public static class SproutSpectreConsoleExtensions
{
    /// <summary>
    /// Registers the SpectreConsoleProgressListener as the IProgressListener implementation, replacing the default ConsoleProgressListener.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddSpectreConsole(this IServiceCollection services)
    {
        services.RemoveConsoleListener();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IProgressListener, SpectreConsoleProgressListener>());
        return services;
    }

    /// <summary>
    /// Registers the SpectreConsoleProgressListener as the IProgressListener implementation, replacing the default ConsoleProgressListener, and allows specifying a custom refresh rate for the console output.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="refreshRate"></param>
    /// <returns></returns>
    public static IServiceCollection AddSpectreConsole(this IServiceCollection services, TimeSpan refreshRate)
    {
        services.RemoveConsoleListener();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IProgressListener, SpectreConsoleProgressListener>(sp =>
        { 
            if(refreshRate >= TimeSpan.FromMilliseconds(100))
            {
                return ActivatorUtilities.CreateInstance<SpectreConsoleProgressListener>(sp);
            }
            return ActivatorUtilities.CreateInstance<SpectreConsoleProgressListener>(sp, refreshRate);
        }));

        return services;
    }
}

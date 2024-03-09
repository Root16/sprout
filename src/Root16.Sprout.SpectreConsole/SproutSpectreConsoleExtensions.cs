using Microsoft.Extensions.DependencyInjection;
using Root16.Sprout.Progress;
using Root16.Sprout.Progress.SpectreConsole;

namespace Root16.Sprout;

public static class SproutSpectreConsoleExtensions
{
    public static IServiceCollection AddSpectreConsole(this IServiceCollection services)
    {
        if (services.IsReadOnly)
        {
            return services;
        }
        var progressListenerToRemove = services.FirstOrDefault(x => x.ServiceType.Name == typeof(IProgressListener).Name);
        if (progressListenerToRemove is not null)
        {
            services.Remove(progressListenerToRemove);
        }
        services.AddSingleton<IProgressListener, SpectreConsoleProgressListener>();
        return services;
    }
}

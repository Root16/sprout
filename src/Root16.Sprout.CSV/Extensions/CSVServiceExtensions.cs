using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Root16.Sprout.CSV.Factories;

namespace Root16.Sprout.CSV;

public static class CSVServiceExtensions
{
    public static IServiceCollection AddSproutCSV(this IServiceCollection services)
    {
        services.TryAddSingleton<ICSVDataSourceFactory, CSVDataSourceFactory>();
        return services;
    }
}

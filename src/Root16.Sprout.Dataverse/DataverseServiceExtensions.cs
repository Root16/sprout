using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Root16.Sprout.DataSources.Dataverse;

namespace Root16.Sprout;

public static class DataverseServiceExtensions
{

    public static IServiceCollection AddSproutDataverse(this IServiceCollection services)
    {
        services.TryAddTransient<DataverseDataSource>();
        services.TryAddTransient<EntityOperationReducer>();
        services.TryAddSingleton<IDataverseDataSourceFactory, DataverseDataSourceFactory>();
        services.TryAddSingleton<IMemoryCache, MemoryCache>();
        services.TryAddSingleton<IOptionSetMapper, OptionSetMapper>();
        return services;
    }

}
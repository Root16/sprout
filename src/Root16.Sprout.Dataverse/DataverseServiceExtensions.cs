using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Root16.Sprout.DataSources.Dataverse;
using Root16.Sprout.DependencyInjection;

namespace Root16.Sprout;

public static class DataverseServiceExtensions
{

    public static IServiceCollection AddSproutDataverse(this IServiceCollection services)
    {
        services.TryAddTransient<DataverseDataSource>();
        services.TryAddTransient<EntityOperationReducer>();
        services.TryAddSingleton<IDataverseDataSourceFactory, DataverseDataSourceFactory>();
        return services;
    }

}
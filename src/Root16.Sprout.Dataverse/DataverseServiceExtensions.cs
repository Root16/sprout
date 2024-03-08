using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Root16.Sprout.Dataverse.DataStores;

namespace Root16.Sprout;

public static class DataverseServiceExtensions
{

    public static IServiceCollection AddSproutDataverse(this IServiceCollection services)
    {
        services.TryAddTransient<DataverseDataStore>();
        services.TryAddTransient<EntityOperationReducer>();
        services.TryAddSingleton<IDataverseDataStoreFactory, DataverseDataStoreFactory>();
        return services;
    }

    public static IServiceCollection AddDataverseDataSource(this IServiceCollection services, string connectionStringName)
    {
        return services.AddTransient(services =>
        {
            var factory = services.GetRequiredService<IDataverseDataStoreFactory>();
            return factory.CreateDataSource(connectionStringName);
        });
    }

    public static IServiceCollection AddDataverseDataSource(this IServiceCollection services, string connectionStringName, Action<DataverseDataStore> initializer)
    {
        return services.AddTransient(services =>
        {
            var factory = services.GetRequiredService<IDataverseDataStoreFactory>();
            var result = factory.CreateDataSource(connectionStringName);
            initializer(result);
            return result;
        });
    }

}
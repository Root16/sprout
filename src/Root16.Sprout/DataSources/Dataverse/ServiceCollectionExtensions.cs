using Microsoft.Extensions.DependencyInjection;

namespace Root16.Sprout.DataSources.Dataverse;

public static class ServiceCollectionExtensions
{
    

    public static IServiceCollection AddDataverseDataSource(this IServiceCollection services, string connectionStringName)
    {
        return services.AddTransient(services =>
        {
            var factory = services.GetRequiredService<IDataverseDataSourceFactory>();
            return factory.CreateDataSource(connectionStringName);
        });
    }

    public static IServiceCollection AddDataverseDataSource(this IServiceCollection services, string connectionStringName, Action<DataverseDataSource> initializer)
    {
        return services.AddTransient(services =>
        {
            var factory = services.GetRequiredService<IDataverseDataSourceFactory>();
            var result = factory.CreateDataSource(connectionStringName);
            initializer(result);
            return result;
        });
    }


}

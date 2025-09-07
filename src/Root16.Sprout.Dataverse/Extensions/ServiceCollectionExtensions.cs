using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Root16.Sprout.DataSources.Dataverse;

namespace Root16.Sprout.Dataverse.Extensions;

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

    public static IServiceCollection AddOrganizationRequestDataSource(this IServiceCollection services, string connectionStringName)
    {
        services.TryAddSingleton<IOrganizationRequestDataSourceFactory, OrganizationRequestDataSourceFactory>();
        return services.AddTransient(services =>
        {
            var factory = services.GetRequiredService<IOrganizationRequestDataSourceFactory>();
            return factory.CreateDataSource(connectionStringName);
        });
    }

    public static IServiceCollection AddOrganizationRequestDataSource(this IServiceCollection services, string connectionStringName, Action<OrganizationRequestDataSource> initializer)
    {
        return services.AddTransient(services =>
        {
            var factory = services.GetRequiredService<IOrganizationRequestDataSourceFactory>();
            var result = factory.CreateDataSource(connectionStringName);
            initializer(result);
            return result;
        });
    }
}

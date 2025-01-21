using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Root16.Sprout.DataSources.Sql;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlDataSource(this IServiceCollection services, string connectionStringName)
    {
        services.TryAddSingleton<ISqlDataSourceFactory, SqlDataSourceFactory>();
        return services.AddTransient(services =>
        {
            var factory = services.GetRequiredService<ISqlDataSourceFactory>();
            return factory.CreateDataSource(connectionStringName);
        });
    }
}

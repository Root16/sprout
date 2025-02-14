using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Root16.Sprout.DataSources.Sql;

public class SqlDataSourceFactory(IServiceProvider serviceProvider) : ISqlDataSourceFactory
{
    private readonly IServiceProvider serviceProvider = serviceProvider;

    public SqlDataSource CreateDataSource(string connectionStringName)
    {
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var ds = new SqlDataSource(config.GetConnectionString(connectionStringName)!, loggerFactory);
        return ds;
    }
}
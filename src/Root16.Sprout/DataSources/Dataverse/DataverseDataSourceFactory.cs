using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Root16.Sprout.DataSources.Dataverse;

namespace Root16.Sprout.DependencyInjection;

public class DataverseDataSourceFactory : IDataverseDataSourceFactory
{
    private readonly IServiceProvider serviceProvider;

    public DataverseDataSourceFactory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public DataverseDataSource CreateDataSource(string connectionStringName)
    {
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        var logger = serviceProvider.GetRequiredService<ILogger<DataverseDataSource>>();
        var serviceClient = new ServiceClient(
            config.GetConnectionString(connectionStringName),
            serviceProvider.GetRequiredService<ILogger<ServiceClient>>()
        );
        var ds = new DataverseDataSource(serviceClient, logger);
        return ds;
    }
}

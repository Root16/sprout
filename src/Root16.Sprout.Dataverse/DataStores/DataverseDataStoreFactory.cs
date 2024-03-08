using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Root16.Sprout.Dataverse.DataStores;

public class DataverseDataStoreFactory : IDataverseDataStoreFactory
{
    private readonly IServiceProvider serviceProvider;

    public DataverseDataStoreFactory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public DataverseDataStore CreateDataSource(string connectionStringName)
    {
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        var logger = serviceProvider.GetRequiredService<ILogger<DataverseDataStore>>();
        var serviceClient = new ServiceClient(
            config.GetConnectionString(connectionStringName),
            serviceProvider.GetRequiredService<ILogger<ServiceClient>>()
        );
        var ds = new DataverseDataStore(serviceClient, logger);
        return ds;
    }
}

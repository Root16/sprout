using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Root16.Sprout.Dataverse.DataSources.Dataverse;

namespace Root16.Sprout.DataSources.Dataverse;

public class DataverseDataSourceFactory(
    IConfiguration configuration,
    ILogger<DataverseDataSource> dataSourceLogger,
    ILogger<ServiceClient> serviceClientLogger) : IDataverseDataSourceFactory
{
    public DataverseDataSource CreateDataSource(string connectionStringName)
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
                               ?? configuration.GetValue<string>(connectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{connectionStringName}' was not found or is empty.");
        }

        var serviceClient = new ServiceClientWithRetry(connectionString, serviceClientLogger);

        return new DataverseDataSource(serviceClient, dataSourceLogger);
    }
}
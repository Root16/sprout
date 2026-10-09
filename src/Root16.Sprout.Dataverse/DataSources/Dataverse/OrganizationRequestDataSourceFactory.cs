using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Root16.Sprout.Dataverse.DataSources.Dataverse;
using System.Configuration;

namespace Root16.Sprout.DataSources.Dataverse;

public class OrganizationRequestDataSourceFactory(IServiceProvider serviceProvider) : IOrganizationRequestDataSourceFactory
{
    public OrganizationRequestDataSource CreateDataSource(string connectionStringName)
    {
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        var connectionString = config.GetConnectionString(connectionStringName)
                       ?? config.GetValue<string>(connectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{connectionStringName}' was not found or is empty.");
        }
        var dsLogger = serviceProvider.GetRequiredService<ILogger<DataverseDataSource>>();
        var serviceClient = new ServiceClientWithRetry(connectionString,
            serviceProvider.GetRequiredService<ILogger<ServiceClient>>()
        );
        var ds = new DataverseDataSource(serviceClient, dsLogger);
        var ordsLogger = serviceProvider.GetRequiredService<ILogger<OrganizationRequestDataSource>>();
        var ords = new OrganizationRequestDataSource(ds, ordsLogger);
        return ords;
    }
}
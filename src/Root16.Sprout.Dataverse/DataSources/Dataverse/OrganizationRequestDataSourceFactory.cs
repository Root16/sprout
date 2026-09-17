using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Root16.Sprout.Dataverse.DataSources.Dataverse;

namespace Root16.Sprout.DataSources.Dataverse;

public class OrganizationRequestDataSourceFactory(IServiceProvider serviceProvider) : IOrganizationRequestDataSourceFactory
{
    public OrganizationRequestDataSource CreateDataSource(string connectionStringName)
    {
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        var dsLogger = serviceProvider.GetRequiredService<ILogger<DataverseDataSource>>();
        var serviceClient = new ServiceClientWithRetry(
            config.GetConnectionString(connectionStringName)!,
            serviceProvider.GetRequiredService<ILogger<ServiceClient>>()
        );
        var ds = new DataverseDataSource(serviceClient, dsLogger);
        var ordsLogger = serviceProvider.GetRequiredService<ILogger<OrganizationRequestDataSource>>();
        var ords = new OrganizationRequestDataSource(ds, ordsLogger);
        return ords;
    }
}
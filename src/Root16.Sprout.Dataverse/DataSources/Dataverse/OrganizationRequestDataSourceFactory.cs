using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Root16.Sprout.DataSources.Dataverse;

public class OrganizationRequestDataSourceFactory(IServiceProvider serviceProvider) : IOrganizationRequestDataSourceFactory
{
    public OrganizationRequestDataSource CreateDataSource(string connectionStringName)
    {
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        var dsLogger = serviceProvider.GetRequiredService<ILogger<DataverseDataSource>>();
        var serviceClient = new ServiceClient(
            config.GetConnectionString(connectionStringName),
            serviceProvider.GetRequiredService<ILogger<ServiceClient>>()
        );
        var ds = new DataverseDataSource(serviceClient, dsLogger);
        var ordsLogger = serviceProvider.GetRequiredService<ILogger<OrganizationRequestDataSource>>();
        var ords = new OrganizationRequestDataSource(ds, ordsLogger);
        return ords;
    }
}
namespace Root16.Sprout.DataSources.Dataverse;

public interface IOrganizationRequestDataSourceFactory
{
    OrganizationRequestDataSource CreateDataSource(string name);
}

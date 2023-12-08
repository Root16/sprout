namespace Root16.Sprout.DataSources.Dataverse;

public interface IDataverseDataSourceFactory
{
    DataverseDataSource CreateDataSource(string name);
}

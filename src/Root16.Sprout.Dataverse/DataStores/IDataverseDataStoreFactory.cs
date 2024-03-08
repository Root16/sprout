namespace Root16.Sprout.Dataverse.DataStores;

public interface IDataverseDataStoreFactory
{
    DataverseDataStore CreateDataSource(string name);
}

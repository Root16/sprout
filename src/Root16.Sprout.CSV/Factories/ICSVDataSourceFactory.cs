namespace Root16.Sprout.CSV.Factories;

public interface ICSVDataSourceFactory
{
    CSVDataSource<T> GetCSVDataSourceByName<T>(string recordName) where T : class;
}

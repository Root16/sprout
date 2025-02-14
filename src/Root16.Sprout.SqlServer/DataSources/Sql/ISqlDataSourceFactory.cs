namespace Root16.Sprout.DataSources.Sql;

public interface ISqlDataSourceFactory
{
    SqlDataSource CreateDataSource(string name);
}
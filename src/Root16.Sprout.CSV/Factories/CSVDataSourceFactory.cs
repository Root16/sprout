using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Root16.Sprout.CSV.Factories;

public class CSVDataSourceFactory(IServiceProvider serviceProvider, ILogger<CSVDataSourceFactory> logger) : ICSVDataSourceFactory
{
    public CSVDataSource<T> GetCSVDataSourceByName<T>(string dataSourceName) where T : class
    {
        return serviceProvider.GetRequiredKeyedService<CSVDataSource<T>>(dataSourceName);
    }
}
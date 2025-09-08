using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Root16.Sprout.Excel.Factories;

public class ExcelDataSourceFactory(IServiceProvider serviceProvider, ILogger<ExcelDataSourceFactory> logger) 
    : IExcelDataSourceFactory
{
    public ExcelDataSource<T> GetExcelDataSourceByName<T>(string dataSourceName) where T : class
    {
        return serviceProvider.GetRequiredKeyedService<ExcelDataSource<T>>(dataSourceName);
    }
}
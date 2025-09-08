using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Root16.Sprout.Excel.Factories;

namespace Root16.Sprout.Excel;

public static class ExcelServiceExtensions
{
    public static IServiceCollection AddSproutExcel(this IServiceCollection services)
    {
        services.TryAddSingleton<IExcelDataSourceFactory, ExcelDataSourceFactory>();
        return services;
    }
}

using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Root16.Sprout.CSV;
using System.Globalization;

namespace Root16.Sprout.DependencyInjection;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterCSVDataSource<T>(this IServiceCollection services, string csvDataSourceName, string path)
        where T : class
    {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = csv.GetRecords<T>().ToList();
        services.AddKeyedSingleton(csvDataSourceName, new CSVDataSource<T>(records));
        return services;
    }
    public static IServiceCollection RegisterCSVDataSource<T, TClassMap>(this IServiceCollection services, string csvDataSourceName, string path)
        where T : class
        where TClassMap : ClassMap<T>
    {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<TClassMap>();
        var records = csv.GetRecords<T>().ToList();
        services.AddKeyedSingleton(csvDataSourceName, new CSVDataSource<T>(records));
        return services;
    }
}

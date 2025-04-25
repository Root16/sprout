using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Root16.Sprout.DependencyInjection;
using System.Globalization;

namespace Root16.Sprout.CSV.DependencyInjection;

public static partial class ServiceCollectionExtensions
{

    public static IServiceCollection RegisterCSVDataSource<TCSVType,TCSVMapType>(this IServiceCollection services, string path)
        where TCSVType : class
        where TCSVMapType : ClassMap<TCSVType>
    {
        services.AddKeyedSingleton(typeof(TCSVType), (serviceProvider, mykey) =>
        {
            return new CSVDataSourceRegistration<TCSVType, TCSVMapType>(path);
        });
        services.AddSingleton<CSVDataSource<TCSVType>>(serviceProvider =>
        {
            CSVDataSourceRegistration<TCSVType, TCSVMapType> registration = serviceProvider.GetRequiredKeyedService<CSVDataSourceRegistration<TCSVType, TCSVMapType>>(typeof(TCSVType));
            using var reader = new StreamReader(registration.Path);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<TCSVMapType>();
            var records = csv.GetRecords<TCSVType>().ToList();
            return new CSVDataSource<TCSVType>(records);
        });
        return services;
    }

    public static IServiceCollection RegisterCSVDataSource<TCSVType, TCSVMapType>(this IServiceCollection services, string path, string csvDataSourceName)
        where TCSVType : class
        where TCSVMapType : ClassMap
    {
        services.AddKeyedSingleton(csvDataSourceName, (serviceProvider, mykey) =>
        {
            return new CSVDataSourceRegistration<TCSVType, TCSVMapType>(path);
        });
        services.AddKeyedSingleton<CSVDataSource<TCSVType>>(csvDataSourceName, (serviceProvider, mykey) =>
        {
            CSVDataSourceRegistration<TCSVType, TCSVMapType> registration = serviceProvider.GetRequiredKeyedService<CSVDataSourceRegistration<TCSVType, TCSVMapType>>(mykey);
            using var reader = new StreamReader(registration.Path);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<TCSVMapType>();
            var records = csv.GetRecords<TCSVType>().ToList();
            return new CSVDataSource<TCSVType>(records);
        });
        return services;
    }
}

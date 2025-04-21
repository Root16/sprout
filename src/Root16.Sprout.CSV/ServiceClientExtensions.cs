using CsvHelper.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Root16.Sprout.CSV;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddCSVDataSouce(this IServiceCollection services, string path, Type csvType, Type csvMapType)
    {

        return services;
    }

    public static IServiceCollection AddCSVDataSource<TCSVType>(this IServiceCollection services, string path, Type csvMapType)
        where TCSVType : class
    {

        return services;
    }

    public static IServiceCollection AddCSVDataSouce<TCSVType,TCSVMapType>(this IServiceCollection services, string path)
        where TCSVType : class
        where TCSVMapType : ClassMap
    {

        return services;
    }

    public static IServiceCollection AddCSVDataSouce<TCSVType, TCSVMapType>(this IServiceCollection services, string path, string csvDataSourceName)
        where TCSVType : class
        where TCSVMapType : ClassMap
    {
        services.AddKeyedSingleton(serviceKey: csvDataSourceName, (serviceProvider, myKey) =>
        {
            return new CSVDataSource<TCSVType>(path)
        });
        return services;
    }
}

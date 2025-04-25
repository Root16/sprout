using CsvHelper.Configuration;

namespace Root16.Sprout.DependencyInjection;

public class CSVDataSourceRegistration<TCSVType, TCSVMapType>(string path)
    where TCSVType : class
    where TCSVMapType : ClassMap
{
    public readonly TCSVType CSVType = Activator.CreateInstance<TCSVType>();
    public readonly TCSVMapType CSVMapType = Activator.CreateInstance<TCSVMapType>();
    public readonly string Path = path;
}

using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Root16.Sprout.DependencyInjection;

public class CSVDataSourceRegistration<TCSVType, TCSVMapType>
    where TCSVType : class
    where TCSVMapType : ClassMap
{
    public string Path { get; }

    public CSVDataSourceRegistration(string path)
    {
        Path = path;
    }
}

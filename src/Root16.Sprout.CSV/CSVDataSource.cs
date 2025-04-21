using CsvHelper;
using Root16.Sprout.DataSources;
using System.Globalization;

namespace Root16.Sprout.CSV
{
    public class CSVDataSource<T> : MemoryDataSource<T> where T : class
    {
        public new List<T> Records { get; }

        public CSVDataSource()
            : base()
        {
            Records = [];
        }

        public CSVDataSource(Type csvMapType, string path)
        {
            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                csv.Context.RegisterClassMap<csvMapType>();
            Records = csv.GetRecords<T>().ToList();
        }
    }
}

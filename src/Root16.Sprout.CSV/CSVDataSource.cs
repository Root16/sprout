using Root16.Sprout.DataSources;

namespace Root16.Sprout.CSV;

public class CSVDataSource<T> : MemoryDataSource<T> where T : class
{
    public new List<T> Records { get; }

    public CSVDataSource() : base()
    {
        Records = [];
    }

    public CSVDataSource(IEnumerable<T> records)
    {
        Records = records.ToList();
    }
}

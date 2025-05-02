using Root16.Sprout.DataSources;

namespace Root16.Sprout.CSV;

public class CSVDataSource<T>(IEnumerable<T> records) : MemoryDataSource<T>(records) where T : class
{
    public override Task<IReadOnlyList<DataOperationResult<T>>> PerformOperationsAsync(IEnumerable<DataOperation<T>> operations, bool dryRun, IEnumerable<string> dataOperationFlags)
    {
        throw new NotImplementedException("CSVDataSource has not been implemented as an output source.");
    }
}

using Root16.Sprout.DataSources;

namespace Root16.Sprout.Excel;

public class ExcelDataSource<T>(IEnumerable<T> records) : MemoryDataSource<T>(records) where T : class
{
    public override Task<IReadOnlyList<DataOperationResult<T>>> PerformOperationsAsync(IEnumerable<DataOperation<T>> operations, bool dryRun, IEnumerable<string> dataOperationFlags)
    {
        throw new NotImplementedException($"{nameof(ExcelDataSource<T>)} has not been implemented as an output source.");
    }
}

using Root16.Sprout.BatchProcessing;

namespace Root16.Sprout.DataSources;

public interface IDataSource<T>
{
    Task<IReadOnlyList<DataOperationResult<T>>> PerformOperationsAsync(IEnumerable<DataOperation<T>> operations, bool dryRun, IEnumerable<string> dataOperationFlags);
}
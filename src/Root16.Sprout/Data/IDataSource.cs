using Root16.Sprout.Processors;

namespace Root16.Sprout.Data;

public interface IDataSource<T>
{
    Task<IReadOnlyList<DataOperationResult<T>>> PerformOperationsAsync(IEnumerable<DataOperation<T>> operations);
}
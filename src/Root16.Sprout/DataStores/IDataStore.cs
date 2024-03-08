namespace Root16.Sprout.DataStores;

public interface IDataStore<TOperation,TOptions> where TOptions: class
{
    Task<IReadOnlyList<OperationResult<TOperation>>> PerformOperationsAsync(IEnumerable<TOperation> operations, TOptions? options = null);
    string GetOperationName(TOperation operation);
}

namespace Root16.Sprout.DataStores;

public class MemoryDataStore<T> : IDataStore<T, MemoryDataStoreOptions>
{
    public List<T> Records { get; }

    public MemoryDataStore(IEnumerable<T> records)
    {
        Records = new List<T>(records);
    }
    public MemoryDataStore()
    {
        Records = new List<T>();
    }

    public IPagedQuery<T> CreatePagedQuery()
    {
        return new MemoryPagedQuery<T>(Records.ToArray());
    }

    public Task<IReadOnlyList<OperationResult<T>>> PerformOperationsAsync(IEnumerable<T> operations, MemoryDataStoreOptions? options)
    {
        Records.AddRange(operations.Select(r => r));

        IReadOnlyList<OperationResult<T>> results = operations
            .Select(r => new OperationResult<T>(r, true))
            .ToList();
        return Task.FromResult(results);
    }

    public string GetOperationName(T operation) => "create";
}

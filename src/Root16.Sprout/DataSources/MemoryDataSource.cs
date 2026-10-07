using System.Collections.Concurrent;

namespace Root16.Sprout.DataSources;

public class MemoryDataSource<T> : IDataSource<T>
{
	public ConcurrentBag<T> Records { get; }

	public MemoryDataSource(IEnumerable<T> records)
	{
		Records = [.. records];
	}
	public MemoryDataSource()
	{
		Records = [];
	}

	public IPagedQuery<T> CreatePagedQuery()
	{
        return new MemoryPagedQuery<T>([.. Records]);
	}

    public virtual Task<IReadOnlyList<DataOperationResult<T>>> PerformOperationsAsync(IEnumerable<DataOperation<T>> operations, bool dryRun, IEnumerable<string> dataOperationFlags)
    {
		foreach (var record in operations.Select(r => r.Data))
		{
			Records.Add(record);
		}
		IReadOnlyList<DataOperationResult<T>> results = [.. operations.Select(r => new DataOperationResult<T>(r, true))];
		return Task.FromResult(results);
	}
}

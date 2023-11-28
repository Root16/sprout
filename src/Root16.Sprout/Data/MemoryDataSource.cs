using Root16.Sprout.Processors;
using Root16.Sprout.Progress;
using Root16.Sprout.Query;
using System.Collections.Generic;
using System.Linq;

namespace Root16.Sprout.Data;

public class MemoryDataSource<T> : IDataSource, IDataOperationEndpoint<T>
{
	public List<T> Records { get; }

	public MemoryDataSource(IEnumerable<T> records)
	{
		Records = new List<T>(records);
	}
	public MemoryDataSource()
	{
		Records = new List<T>();
	}

	public IPagedQuery<T> CreatePagedQuery()
	{
		return new MemoryPagedQuery<T>(Records.ToArray());
	}

    public Task<IReadOnlyList<DataOperationResult<T>>> PerformOperationsAsync(IEnumerable<DataOperation<T>> operations)
    {
		Records.AddRange(operations.Select(r => r.Data));
		IReadOnlyList<DataOperationResult<T>> results = operations
			.Select(r => new DataOperationResult<T>(r, true))
			.ToList();
		return Task.FromResult(results);
	}
}

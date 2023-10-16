using Root16.Sprout.Progress;
using Root16.Sprout.Query;
using System.Collections.Generic;
using System.Linq;

namespace Root16.Sprout.Data;

public class MemoryDataSource<T> : IDataSource, IDataSink<T>
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

	public IReadOnlyList<DataChangeType> Update(IEnumerable<DataChange<T>> records)
	{
		Records.AddRange(records.Select(r => r.Target));
		return records.Select(r => DataChangeType.Create).ToList();
	}

}

using System.Collections.Generic;
using System.Linq;

namespace Root16.Sprout.Query;

public class MemoryPagedQuery<T> : IPagedQuery<T>
{
	private readonly List<T> data;
	int pos = 0;

	public MemoryPagedQuery(IEnumerable<T> data)
	{
		this.data = data.ToList();
		MoreRecords = this.data.Count > 0;
	}
	public bool MoreRecords { get; private set; }

	public IReadOnlyList<T> GetNextPage(int pageSize)
	{
		var results = data.Skip(pos).Take(pageSize).ToList();
		pos += results.Count;
		MoreRecords = pos < data.Count;
		return results;
	}

	public int? GetTotalRecordCount()
	{
		return data.Count;
	}
}

using System;
using System.Collections.Generic;

namespace Root16.Sprout.Query;

// TODO: make async
public interface IPagedQuery<T>
{
	bool MoreRecords { get; }
	IReadOnlyList<T> GetNextPage(int pageSize);
	int? GetTotalRecordCount();
}
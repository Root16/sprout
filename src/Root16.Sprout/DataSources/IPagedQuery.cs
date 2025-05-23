﻿namespace Root16.Sprout.DataSources;

public interface IPagedQuery<T>
{
	Task<PagedQueryResult<T>> GetNextPageAsync(int pageNumber, int pageSize, object? bookmark);
	Task<int?> GetTotalRecordCountAsync(int batchSize, int? maxBatchCount);
}

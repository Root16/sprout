namespace Root16.Sprout.DataStores;

public interface IPagedQuery<T>
{
    Task<PagedQueryResult<T>> GetNextPageAsync(int pageNumber, int pageSize, object? bookmark);
    Task<int?> GetTotalRecordCountAsync();
}

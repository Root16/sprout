namespace Root16.Sprout.DataStores;

public class MemoryPagedQuery<T> : IPagedQuery<T>
{
    private readonly List<T> data;

    public MemoryPagedQuery(IEnumerable<T> data)
    {
        this.data = data.ToList();
    }

    public Task<PagedQueryResult<T>> GetNextPageAsync(int pageNumber, int pageSize, object? bookmark)
    {
        var results = data.Skip(pageNumber * pageSize).Take(pageSize).ToList();

        return Task.FromResult(new PagedQueryResult<T>
        (
            results,
            (pageNumber + 1) * pageSize < data.Count,
            null
        ));
    }

    public Task<int?> GetTotalRecordCountAsync()
    {
        return Task.FromResult((int?)data.Count);
    }
}

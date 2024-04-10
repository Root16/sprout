
namespace Root16.Sprout.DataSources;

public class MemoryPagedQuery<T>(IEnumerable<T> data) : IPagedQuery<T>
{
	private readonly List<T> data = data.ToList();

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

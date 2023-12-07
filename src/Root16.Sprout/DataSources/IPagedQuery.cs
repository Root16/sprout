namespace Root16.Sprout.DataSources;

// TODO: make async
public interface IPagedQuery<T>
{
	bool MoreRecords { get; }
	IReadOnlyList<T> GetNextPage(int pageSize);
	int? GetTotalRecordCount();
}
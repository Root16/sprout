namespace Root16.Sprout.DataSources;

public class PagedQueryState
{
    public int? TotalRecordCount { get; set; }
    public bool MoreRecords { get; set; }
    public object? State { get; set; }
}


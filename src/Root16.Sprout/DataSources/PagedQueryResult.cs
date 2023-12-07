namespace Root16.Sprout.DataSources;

public record PagedQueryResult<T>
(
    IReadOnlyList<T> Records,
    bool MoreRecords,
    object? Bookmark
);

public record PagedQueryState<T>
(
    int NextPageNumber,
    int RecordsPerPage,
    int RecordsProcessed,
    int? TotalRecordCount,
    bool MoreRecords,
    object? Bookmark
);

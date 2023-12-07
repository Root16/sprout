namespace Root16.Sprout.DataSources;

public record PagedQueryResult<T>
(
    IReadOnlyList<T> Records,
    bool MoreRecords,
    object? Bookmark
);

public record PagedQueryState<T>
(
    bool MoreRecords,
    int NextPageNumber,
    int? TotalRecordCount,
    int RecordsPerPage,
    object? Bookmark
);

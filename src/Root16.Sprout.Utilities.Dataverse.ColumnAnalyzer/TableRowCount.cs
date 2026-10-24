namespace Root16.Sprout.Utilities.Dataverse.ColumnAnalyzer;

public record TableRowCount
{
    public required string Table { get; set; }
    public int RowCount { get; set; }
}

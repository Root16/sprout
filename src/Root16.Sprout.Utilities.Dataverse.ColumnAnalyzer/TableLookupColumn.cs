namespace Root16.Sprout.Utilities.Dataverse.ColumnAnalyzer;

public record TableLookupColumn
{
    public required string Table { get; set; }
    public required string Column { get; set; }
    public required string LookupTable { get; set; }
    public bool LookupTableIsMissing { get; set; }
}

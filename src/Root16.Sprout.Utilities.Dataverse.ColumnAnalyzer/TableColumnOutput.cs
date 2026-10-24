namespace Root16.Sprout.Utilities.Dataverse.ColumnAnalyzer;

public record TableColumnOutput
{
    public required string Table { get; set; }
    public required string Column { get; set; }
    public string? ColumnLabel { get; set; }
    public bool IsIncluded { get; set; }
    public string? ExcludedBy { get; set; }
    public bool IsValidForCreate { get; set; }
    public bool IsValidForUpdate { get; set; }
    public bool IsValidForRead { get; set; }
    public string? CalculationType { get; set; }
    public required string AttributeType { get; set; }
    public int? ColumnCount { get; set; }
    public string? Note { get; set; }
}

namespace Root16.Sprout.Utilities.Dataverse.ColumnAnalyzer;

public record Settings
{
    public required List<string> TableLogicalNames { get; set; }
    public required string FolderPath { get; set; }
    public bool GetRowCounts { get; set; } = false;
    public bool GetColumnUsageCounts { get; set; } = false;
    public bool DetermineStepOrder { get; set; } = false;
    public List<string> ExcludeKeywords { get; set; } = [];
}

namespace Root16.Sprout.Logging;


public record Audit(string TableName, string PrimaryKey, Dictionary<string, ChangeValue> Changes);
public record ChangeValue(string? PreviousValue, string? NewValue);
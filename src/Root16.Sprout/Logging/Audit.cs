namespace Root16.Sprout.Logging;


public record Audit(string TableName, string PrimaryKey, string? AlternateKey, Dictionary<string, ChangeValue> Changes);
public record ChangeValue(string? PreviousValue, string? NewValue);
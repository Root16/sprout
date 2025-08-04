using Root16.Sprout.DataSources;
using Root16.Sprout.Extensions;
using System.Text;
using System.Text.Json;

namespace Root16.Sprout.Logging;

public abstract class BatchAnalyzer<T> where T : class
{
    public DateTimeKind TimeFormat { get; set; } = DateTimeKind.Utc;
    private string timeFormatString { get { return TimeFormat != DateTimeKind.Utc ? "yyyy-MM-ddTHH:mm:sszzz": "u"; } }

    public virtual void ReportFailures(
        string outputFilePath,
        IReadOnlyList<DataOperationResult<T>> results,
        Func<T, string>? keySelector = null
        )
    {
        if (string.IsNullOrWhiteSpace(outputFilePath)) throw new ArgumentNullException($"Missing {nameof(outputFilePath)}.");

        var sb = new StringBuilder();
        foreach (var result in results)
        {
            if (result.WasSuccessful == false)
            {
                HashSet<string> keys = [result.PrimaryKey ?? "Unknown primary key", keySelector?.Invoke(result.Operation.Data) ?? "Unknown key via key selector"];
                var (operation, tableName, keyExpression) = (result.Operation.OperationType, result.TableName ?? "Unknown Table", string.Join(", ", keys ?? ["Unknown Key"]));

                sb.AppendLine($"[{DateTimeOffset.Now.ToString(timeFormatString)}] Target: {tableName}. Keys: {keyExpression}. Operation: {operation}. Error: {result.ErrorMessage}");
            }
        }
        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath!));
        using (StreamWriter outputFile = new StreamWriter(outputFilePath, append: true))
        {
            if (!string.IsNullOrWhiteSpace(sb.ToString()))
                outputFile.Write(sb.ToString());
        }
    }

    public virtual void ReportDifferences(
        string outputFilePath,
        IReadOnlyList<DataOperationResult<T>> results,
        IReadOnlyList<T> originals,
        Func<T, string> keySelector,
        StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
    {
        if (string.IsNullOrWhiteSpace(outputFilePath)) throw new ArgumentNullException($"Missing {nameof(outputFilePath)}.");

        originals ??= [];
        Dictionary<string, List<T>> potentialMatches = originals.GroupBy(keySelector!).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.FromComparison(stringComparison));

        var changes = new List<ChangeRecord>();
        var sb = new StringBuilder();
        foreach (var result in results.Where(r => r.WasSuccessful))
        {
            HashSet<string> keys = [result.PrimaryKey ?? "Unknown primary key", keySelector?.Invoke(result.Operation.Data) ?? "Unknown key via key selector"];
            var (data, operation, tableName, keyExpression) = (result.Operation.Data, result.Operation.OperationType, result.TableName ?? "Unknown Table", string.Join(", ", keys ?? ["Unknown Key"]));

            var matches = potentialMatches.GetValue(keySelector!(data));
            if (matches is not null && matches.Any())
            {
                if (matches.Count > 1)
                {
                    sb.AppendLine($"[{DateTimeOffset.Now.ToString(timeFormatString)}] Target: {tableName}. Keys: {keyExpression}. Operation: {operation}. Error: {result.ErrorMessage}");
                    continue;
                }

                var match = matches[0];
                var changeRecord = GetDifference(keyExpression, data, match);
                sb.AppendLine($"[{DateTimeOffset.Now.ToString(timeFormatString)}] Target: {tableName}. Keys: {keyExpression}. Operation: {operation}. Difference: {JsonSerializer.Serialize(changeRecord)}");
            }
            else
            {
                var changeRecord = GetDifference(keyExpression, data);
                sb.AppendLine($"[{DateTimeOffset.Now.ToString(timeFormatString)}] Target: {tableName}. Keys: {keyExpression}. Operation: {operation}. Difference: {JsonSerializer.Serialize(changeRecord)}");
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath!));
        using (StreamWriter outputFile = new StreamWriter(outputFilePath, append: true))
        {
            if (!string.IsNullOrWhiteSpace(sb.ToString()))
                outputFile.Write(sb.ToString());
        }
    }

    protected abstract ChangeRecord GetDifference(string key, T data, T? previousData = null);

    public record ChangeRecord(string TableName, string PrimaryKey, Dictionary<string, ChangeValue> Changes);
    public record ChangeValue(string? PreviousValue, string? NewValue);
}

using Microsoft.Extensions.Logging;
using Root16.Sprout.DataSources;
using System.Text;

namespace Root16.Sprout.Logging;

public class BatchLogger(ILogger<BatchLogger> logger)
{
    public DateTimeKind TimeFormat { get; set; } = DateTimeKind.Local;
    private string timeFormatString { get { return TimeFormat != DateTimeKind.Utc ? "yyyy-MM-ddTHH:mm:sszzz" : "u"; } }


    public void LogFailures<TOutput>(
        IReadOnlyList<DataOperationResult<TOutput>> results,
        Func<TOutput, string>? keySelector = null
        )
    {
        foreach (var result in results)
        {
            if (result.WasSuccessful == false)
            {
                var tableName = result.TableName ?? "Unknown Table";

                HashSet<string> keys = [result.PrimaryKey ?? "Unknown primary key", keySelector?.Invoke(result.Operation.Data) ?? "Unknown key via key selector"];

                var keyExpression = string.Join(", ", keys ?? ["Unknown Key"]);

                logger.LogError($"Target: {tableName}. Keys: {keyExpression}. Error: {result.ErrorMessage}");
            }
        }
    }

    public virtual void ReportFailures<TOutput>(
        string outputFilePath,
        IReadOnlyList<DataOperationResult<TOutput>> results,
        Func<TOutput, string>? keySelector = null
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

        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath)!);
        using (StreamWriter outputFile = new StreamWriter(outputFilePath, append: true))
        {
            if (!string.IsNullOrWhiteSpace(sb.ToString()))
                outputFile.Write(sb.ToString());
        }
    }

    public virtual void ReportDifferences<TOutput>(
        string outputFilePath,
        IReadOnlyList<DataOperationResult<TOutput>> results,
        Func<TOutput, string>? keySelector = null
        )
    {
        if (string.IsNullOrWhiteSpace(outputFilePath)) throw new ArgumentNullException($"Missing {nameof(outputFilePath)}.");

        var sb = new StringBuilder();
        foreach (var result in results.Where(r => r.WasSuccessful))
        {
            HashSet<string> keys = [result.PrimaryKey ?? "Unknown primary key", keySelector?.Invoke(result.Operation.Data) ?? "Unknown key via key selector"];
            var (data, operation, tableName, keyExpression) = (result.Operation.Data, result.Operation.OperationType, result.TableName ?? "Unknown Table", string.Join(", ", keys ?? ["Unknown Key"]));

            var audit = result.Operation.Audit;
            sb.AppendLine($"[{DateTimeOffset.Now.ToString(timeFormatString)}] Target: {tableName}. Keys: {keyExpression}. Operation: {operation}. Difference: {System.Text.Json.JsonSerializer.Serialize(audit)}");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath)!);
        using (StreamWriter outputFile = new StreamWriter(outputFilePath, append: true))
        {
            if (!string.IsNullOrWhiteSpace(sb.ToString()))
                outputFile.Write(sb.ToString());
        }
    }
}

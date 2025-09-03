using Microsoft.Extensions.Logging;
using Root16.Sprout.DataSources;
using System.Text;

namespace Root16.Sprout.Logging;

public class BatchLogger(ILogger<BatchLogger> logger)
{
	private int totalSuccessfulCreates = 0;
	private int totalSuccessfulUpdates = 0;
	private int totalFailedCreates = 0;
	private int totalFailedUpdates = 0;

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
	public void UpdateTotals<TOutput>(IReadOnlyList<DataOperationResult<TOutput>> results)
	{
		foreach (var r in results)
		{
			if (r.WasSuccessful)
			{
				if (r.Operation.OperationType == "Create")
					totalSuccessfulCreates++;
				else if (r.Operation.OperationType == "Update")
					totalSuccessfulUpdates++;
			}
			else
			{
				if (r.Operation.OperationType == "Create")
					totalFailedCreates++;
				else if (r.Operation.OperationType == "Update")
					totalFailedUpdates++;
			}
		}
	}
	public void LogTotalsAndReset(string stepName)
	{
		logger.LogInformation($"{stepName}: {nameof(totalSuccessfulCreates)}: {totalSuccessfulCreates}");
		logger.LogInformation($"{stepName}: {nameof(totalSuccessfulUpdates)}: {totalSuccessfulUpdates}");
		logger.LogInformation($"{stepName}: {nameof(totalFailedCreates)}: {totalFailedCreates}");
		logger.LogInformation($"{stepName}: {nameof(totalFailedUpdates)}: {totalFailedUpdates}");

		totalSuccessfulCreates = 0;
		totalSuccessfulUpdates = 0;
		totalFailedCreates = 0;
		totalFailedUpdates = 0;
	}
	public void ReportFailuresToFile<TOutput>(
		string outputFilePath,
		IReadOnlyList<DataOperationResult<TOutput>> results,
		Func<TOutput, string>? keySelector = null,
		string? dateTimeFormat = "u"
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

				sb.AppendLine($"[{DateTimeOffset.UtcNow.ToString(dateTimeFormat)}] Target: {tableName}. Keys: {keyExpression}. Operation: {operation}. Error: {result.ErrorMessage}");
			}
		}

		Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath)!);
		using (StreamWriter outputFile = new StreamWriter(outputFilePath, append: true))
		{
			if (!string.IsNullOrWhiteSpace(sb.ToString()))
				outputFile.Write(sb.ToString());
		}
	}

	public void ReportDifferencesToFile<TOutput>(
		string outputFilePath,
		IReadOnlyList<DataOperationResult<TOutput>> results,
		Func<TOutput, string>? keySelector = null,
		string? dateTimeFormat = "u"
		)
	{
		if (string.IsNullOrWhiteSpace(outputFilePath)) throw new ArgumentNullException($"Missing {nameof(outputFilePath)}.");

		var sb = new StringBuilder();
		foreach (var result in results.Where(r => r.WasSuccessful))
		{
			HashSet<string> keys = [result.PrimaryKey ?? "Unknown primary key", keySelector?.Invoke(result.Operation.Data) ?? "Unknown key via key selector"];
			var (data, operation, tableName, keyExpression) = (result.Operation.Data, result.Operation.OperationType, result.TableName ?? "Unknown Table", string.Join(", ", keys ?? ["Unknown Key"]));

			var audit = result.Operation.Audit;
			sb.AppendLine($"[{DateTimeOffset.UtcNow.ToString(dateTimeFormat)}] Target: {tableName}. Keys: {keyExpression}. Operation: {operation}. Difference: {System.Text.Json.JsonSerializer.Serialize(audit)}");
		}

		Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath)!);
		using (StreamWriter outputFile = new StreamWriter(outputFilePath, append: true))
		{
			if (!string.IsNullOrWhiteSpace(sb.ToString()))
				outputFile.Write(sb.ToString());
		}
	}
}

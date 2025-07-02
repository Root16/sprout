using Microsoft.Extensions.Logging;
using Root16.Sprout.DataSources;

namespace Root16.Sprout.Logging;

public class BatchLogger(ILogger<BatchLogger> logger)
{
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
}

namespace Root16.Sprout.DataSources;

public record DataOperationResult<T>(
	DataOperation<T> Operation, 
	bool WasSuccessful, 
	string? PrimaryKey = null,
	string? TableName = null,
	string? ErrorMessage = null
	);


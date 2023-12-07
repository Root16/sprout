namespace Root16.Sprout.DataSources;

public record DataOperationResult<T>(DataOperation<T> Operation, bool WasSuccessful);


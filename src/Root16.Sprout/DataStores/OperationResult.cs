namespace Root16.Sprout.DataStores;

public record OperationResult<T>(T Operation, bool WasSuccessful);
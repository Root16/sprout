namespace Root16.Sprout.DataSources;

public record DataOperation<T>(OperationType? OperationType, T Data);

public enum OperationType
{
    Create,
    Update
}
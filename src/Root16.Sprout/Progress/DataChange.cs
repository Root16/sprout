namespace Root16.Sprout.Progress;

public class DataChange<T>
{
	public T? Target { get; set; }
	public DataChangeType Type { get; set; }
}
using Root16.Sprout.Logging;

namespace Root16.Sprout.DataSources;

public record DataOperation<T>(string OperationType, T Data)
{
    public Audit? Audit { get; set; }

    public DataOperation(string OperationType, T Data, Audit? Audit) : this(OperationType, Data)
    {
        this.Audit = Audit;
    }
}

using Root16.Sprout.DataSources;

namespace Root16.Sprout.BatchProcessing;

public abstract class BatchIntegrationStep<TInput, TOutput> : IBatchIntegrationStep<TInput, TOutput>
{
    protected BatchIntegrationStep()
    {
        BatchSize = 200;
    }

    public bool DryRun { get; set; }
    public int BatchSize { get; set; }
	public Func<TOutput, string>? KeySelector { get; init; }
    public TimeSpan? BatchDelay { get; set; }

    private readonly HashSet<string> dataOperationFlags = new(StringComparer.OrdinalIgnoreCase);
    public IEnumerable<string> DataOperationFlags { get { return dataOperationFlags; } }


    public void AddDataOperationFlag(string flag) => dataOperationFlags.Add(flag);
    public void RemoveDataOperationFlag(string flag) => dataOperationFlags.Remove(flag);

    public abstract IDataSource<TOutput> OutputDataSource { get; }
    public abstract IPagedQuery<TInput> GetInputQuery();
    public abstract IReadOnlyList<DataOperation<TOutput>> MapRecord(TInput source);
    public virtual Task OnAfterDeliveryAsync(IReadOnlyList<DataOperationResult<TOutput>> results)
    {
        OnAfterDelivery(results);
        return Task.CompletedTask;
    }
    public virtual Task<IReadOnlyList<DataOperation<TOutput>>> OnAfterMapAsync(IReadOnlyList<DataOperation<TOutput>> batch) => Task.FromResult(OnAfterMap(batch));
    public virtual Task<IReadOnlyList<DataOperation<TOutput>>> OnBeforeDeliveryAsync(IReadOnlyList<DataOperation<TOutput>> batch) => Task.FromResult(OnBeforeDelivery(batch));
    public virtual Task<IReadOnlyList<TInput>> OnBeforeMapAsync(IReadOnlyList<TInput> batch) => Task.FromResult(OnBeforeMap(batch));
    
    public virtual void OnAfterDelivery(IReadOnlyList<DataOperationResult<TOutput>> results) { }
    public virtual IReadOnlyList<DataOperation<TOutput>> OnAfterMap(IReadOnlyList<DataOperation<TOutput>> batch) => batch;
    public virtual IReadOnlyList<DataOperation<TOutput>> OnBeforeDelivery(IReadOnlyList<DataOperation<TOutput>> batch) => batch;
    public virtual IReadOnlyList<TInput> OnBeforeMap(IReadOnlyList<TInput> batch) => batch;
    public abstract Task RunAsync(string stepName);
    public virtual void OnStepStart() { }
    public virtual void OnStepFinished() { }
	public virtual void OnStepError() { }
}


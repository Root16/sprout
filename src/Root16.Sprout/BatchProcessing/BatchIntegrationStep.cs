using Root16.Sprout.DataStores;

namespace Root16.Sprout.BatchProcessing;

public abstract class BatchIntegrationStep<TInput, TOutput, TDataStoreOptions> : IBatchIntegrationStep<TInput, TOutput, TDataStoreOptions>
    where TDataStoreOptions : class
{
    protected BatchIntegrationStep()
    {
        BatchSize = 200;
    }

    public bool DryRun { get; set; }
    public int BatchSize { get; set; }
    
    private HashSet<string> dataOperationFlags = new(StringComparer.OrdinalIgnoreCase);
    public IEnumerable<string> DataOperationFlags { get { return dataOperationFlags; } }
    public TDataStoreOptions? Options { get; set; }


    public void AddDataOperationFlag(string flag) => dataOperationFlags.Add(flag);
    public void RemoveDataOperationFlag(string flag) => dataOperationFlags.Remove(flag);

    public abstract IDataStore<TOutput,TDataStoreOptions> OutputDataStore { get; }
    public abstract IBatchProcessor BatchProcessor { get; }
    public abstract IPagedQuery<TInput> GetInputQuery();
    public abstract IReadOnlyList<TOutput> MapRecord(TInput source);
    public virtual Task OnAfterDeliveryAsync(IReadOnlyList<OperationResult<TOutput>> results)
    {
        OnAfterDelivery(results);
        return Task.CompletedTask;
    }
    public virtual Task<IReadOnlyList<TOutput>> OnAfterMapAsync(IReadOnlyList<TOutput> batch) => Task.FromResult(OnAfterMap(batch));
    public virtual Task<IReadOnlyList<TOutput>> OnBeforeDeliveryAsync(IReadOnlyList<TOutput> batch) => Task.FromResult(OnBeforeDelivery(batch));
    public virtual Task<IReadOnlyList<TInput>> OnBeforeMapAsync(IReadOnlyList<TInput> batch) => Task.FromResult(OnBeforeMap(batch));
    
    public virtual void OnAfterDelivery(IReadOnlyList<OperationResult<TOutput>> results) { }
    public virtual IReadOnlyList<TOutput> OnAfterMap(IReadOnlyList<TOutput> batch) => batch;
    public virtual IReadOnlyList<TOutput> OnBeforeDelivery(IReadOnlyList<TOutput> batch) => batch;
    public virtual IReadOnlyList<TInput> OnBeforeMap(IReadOnlyList<TInput> batch) => batch;

    public virtual Task RunAsync()
    {
        return BatchProcessor.ProcessAllBatchesAsync(this);
    }
}


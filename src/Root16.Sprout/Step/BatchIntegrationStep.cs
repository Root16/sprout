using Root16.Sprout.Data;
using Root16.Sprout.Processors;
using Root16.Sprout.Query;

namespace Root16.Sprout.Step;

public abstract class BatchIntegrationStep<TInput, TOutput> : IBatchIntegrationStep<TInput, TOutput>
{
    protected BatchIntegrationStep()
    {
    }

    public abstract IDataSource<TOutput> OutputDataSource { get; }
    public abstract IPagedQuery<TInput> GetSourceQuery();
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

    public abstract Task RunAsync();
}


using Root16.Sprout.DataSources;

namespace Root16.Sprout.BatchProcessing;

public interface IBatchIntegrationStep<TInput, TOutput> : IIntegrationStep
{
    IDataSource<TOutput> OutputDataSource { get; }
    IPagedQuery<TInput> GetInputQuery();
	IReadOnlyList<DataOperation<TOutput>> MapRecord(TInput input);

    Task<IReadOnlyList<TInput>> OnBeforeMapAsync(IReadOnlyList<TInput> batch);
    Task<IReadOnlyList<DataOperation<TOutput>>> OnAfterMapAsync(IReadOnlyList<DataOperation<TOutput>> batch);
    Task<IReadOnlyList<DataOperation<TOutput>>> OnBeforeDeliveryAsync(IReadOnlyList<DataOperation<TOutput>> batch);
    Task OnAfterDeliveryAsync(IReadOnlyList<DataOperationResult<TOutput>> results);
}



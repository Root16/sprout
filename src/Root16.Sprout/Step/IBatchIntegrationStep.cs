using Root16.Sprout.Data;
using Root16.Sprout.Processors;
using Root16.Sprout.Progress;
using Root16.Sprout.Query;

namespace Root16.Sprout.Step;

public interface IBatchIntegrationStep<TInput, TOutput> : IIntegrationStep
{
    IDataSource<TOutput> OutputDataSource { get; }
    IPagedQuery<TInput> GetSourceQuery();
	IReadOnlyList<DataOperation<TOutput>> MapRecord(TInput input);

    Task<IReadOnlyList<TInput>> OnBeforeMapAsync(IReadOnlyList<TInput> batch);
    Task<IReadOnlyList<DataOperation<TOutput>>> OnAfterMapAsync(IReadOnlyList<DataOperation<TOutput>> batch);
    Task<IReadOnlyList<DataOperation<TOutput>>> OnBeforeDeliveryAsync(IReadOnlyList<DataOperation<TOutput>> batch);
    Task OnAfterDeliveryAsync(IReadOnlyList<DataOperationResult<TOutput>> results);
}



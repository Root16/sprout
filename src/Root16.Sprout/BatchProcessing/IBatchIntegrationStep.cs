using Root16.Sprout.DataStores;

namespace Root16.Sprout.BatchProcessing;

public interface IBatchIntegrationStep<TInput, TOutput, TDataStoreOptions> : IIntegrationStep where TDataStoreOptions : class
{
    IDataStore<TOutput,TDataStoreOptions> OutputDataStore { get; }
    IPagedQuery<TInput> GetInputQuery();
	IReadOnlyList<TOutput> MapRecord(TInput input);

    int BatchSize { get; }
    bool DryRun { get; }
    IEnumerable<string> DataOperationFlags { get; }
    TDataStoreOptions? Options { get; }

    Task<IReadOnlyList<TInput>> OnBeforeMapAsync(IReadOnlyList<TInput> batch);
    Task<IReadOnlyList<TOutput>> OnAfterMapAsync(IReadOnlyList<TOutput> batch);
    Task<IReadOnlyList<TOutput>> OnBeforeDeliveryAsync(IReadOnlyList<TOutput> batch);
    Task OnAfterDeliveryAsync(IReadOnlyList<OperationResult<TOutput>> results);
}



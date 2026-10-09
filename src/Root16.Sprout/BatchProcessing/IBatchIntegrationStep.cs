using Root16.Sprout.DataSources;

namespace Root16.Sprout.BatchProcessing;

public interface IBatchIntegrationStep<TInput, TOutput> : IIntegrationStep
{
    IDataSource<TOutput> OutputDataSource { get; }
	/// <summary>
	/// Lambda for seleting the primary key by which to perform matching between input and output records.
	/// </summary>
	Func<TOutput, string>? KeySelector { get; init; }
    IPagedQuery<TInput> GetInputQuery();
	IReadOnlyList<DataOperation<TOutput>> MapRecord(TInput input);

    int BatchSize { get; }
    TimeSpan? BatchDelay { get; }
    bool DryRun { get; }
    IEnumerable<string> DataOperationFlags { get; }

    Task<IReadOnlyList<TInput>> OnBeforeMapAsync(IReadOnlyList<TInput> batch);
    Task<IReadOnlyList<DataOperation<TOutput>>> OnAfterMapAsync(IReadOnlyList<DataOperation<TOutput>> batch);
    Task<IReadOnlyList<DataOperation<TOutput>>> OnBeforeDeliveryAsync(IReadOnlyList<DataOperation<TOutput>> batch);
    Task OnAfterDeliveryAsync(IReadOnlyList<DataOperationResult<TOutput>> results);
    void OnStepStart();
    void OnStepFinished();
    void OnStepError();
}



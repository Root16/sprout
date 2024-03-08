using Root16.Sprout.DataStores;
using Root16.Sprout.Progress;

namespace Root16.Sprout.BatchProcessing;

public interface IBatchProcessor
{
    Task ProcessAllBatchesAsync<TInput, TOutput, TDataStoreOptions>(IBatchIntegrationStep<TInput, TOutput, TDataStoreOptions> step) where TDataStoreOptions : class;
    Task<BatchState<TInput>> ProcessBatchAsync<TInput, TOutput, TDataStoreOptions>(IBatchIntegrationStep<TInput, TOutput, TDataStoreOptions> step, BatchState<TInput>? batchState, TDataStoreOptions? options = null) where TDataStoreOptions : class;
}

internal class BatchProcessor : IBatchProcessor
{
    public BatchProcessor(IProgressListener progressListener)
    {
        this.progressListener = progressListener;
    }

    private readonly IProgressListener progressListener;

    public async Task ProcessAllBatchesAsync<TInput, TOutput, TDataStoreOptions>(
        IBatchIntegrationStep<TInput, TOutput, TDataStoreOptions> step) where TDataStoreOptions : class
    {
        BatchState<TInput>? batchState = null;
        do
        {
            batchState = await ProcessBatchAsync(step, batchState, step.Options);
        }
        while (batchState.QueryState?.MoreRecords == true);

    }

    public async Task<BatchState<TInput>> ProcessBatchAsync<TInput, TOutput, TDataStoreOptions>(
        IBatchIntegrationStep<TInput, TOutput, TDataStoreOptions> step,
        BatchState<TInput>? batchState, TDataStoreOptions? options = null) where TDataStoreOptions : class
    {

        var queryState = batchState?.QueryState;
        var progress = batchState?.Progress;

        // initialize state if needed
        var query = step.GetInputQuery();
        if (queryState is null)
        {
            var total = await query.GetTotalRecordCountAsync();
            queryState = new(0, step.BatchSize, 0, total, true, null);
        }

        if (progress is null)
        {
            progress = new IntegrationProgress(step.GetType().Name, queryState.TotalRecordCount);
        }

        // get batch of data (IPagedQuery)
        var result = await query.GetNextPageAsync(queryState.NextPageNumber, queryState.RecordsPerPage, queryState.Bookmark);
        var batch = result.Records;
        var proccessedCount = batch.Count;

        batch = await step.OnBeforeMapAsync(batch);

        IReadOnlyList<TOutput> data = new List<TOutput>(batch.SelectMany(step.MapRecord));

        data = await step.OnAfterMapAsync(data);

        data = await step.OnBeforeDeliveryAsync(data);
        var results = await step.OutputDataStore.PerformOperationsAsync(data, options);
        await step.OnAfterDeliveryAsync(results);

        // report progress (IProgressListener)
        progress.AddOperations(proccessedCount, results.Select(r => r.WasSuccessful ? step.OutputDataStore.GetOperationName(r.Operation) : "Error"));
        progressListener.OnProgressChange(progress);


        // return state (more records, paging details) from IPagedQuery

        return new BatchState<TInput>
        (
            new PagedQueryState<TInput>
            (
                queryState.NextPageNumber + 1,
                queryState.RecordsPerPage,
                proccessedCount,
                queryState.TotalRecordCount,
                result.MoreRecords,
                queryState.Bookmark
            ),
            progress
        );
    }
}


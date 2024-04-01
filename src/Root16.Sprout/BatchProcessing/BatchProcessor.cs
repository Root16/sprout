using Root16.Sprout.DataSources;
using Root16.Sprout.Progress;

namespace Root16.Sprout.BatchProcessing;

public class BatchProcessor
{
    public BatchProcessor(IProgressListener progressListener)
    {
        this.progressListener = progressListener;
    }

    private readonly IProgressListener progressListener;

    public async Task ProcessAllBatchesAsync<TInput, TOutput>(
        IBatchIntegrationStep<TInput, TOutput> step)
    {
        BatchState<TInput>? batchState = null;
        do
        {
            batchState = await ProcessBatchAsync(step, batchState);
        }
        while (batchState.QueryState?.MoreRecords == true);

    }

    public async Task<BatchState<TInput>> ProcessBatchAsync<TInput, TOutput>(
        IBatchIntegrationStep<TInput,TOutput> step,
        BatchState<TInput>? batchState)
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

        IReadOnlyList<DataOperation<TOutput>> data = new List<DataOperation<TOutput>>(batch.SelectMany(step.MapRecord));

        data = await step.OnAfterMapAsync(data);

        data = await step.OnBeforeDeliveryAsync(data);
        var results = await step.OutputDataSource.PerformOperationsAsync(data, step.DryRun, step.DataOperationFlags);
        await step.OnAfterDeliveryAsync(results);

        // report progress (IProgressListener)
        progress.AddOperations(proccessedCount, results.Select(r => r.WasSuccessful ? (r.Operation.OperationType?.ToString() ?? "Error") : "Error"));
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


using Root16.Sprout.DataSources;
using Root16.Sprout.Logging;
using Root16.Sprout.Progress;

namespace Root16.Sprout.BatchProcessing;

public class BatchProcessor(
    IProgressListener progressListener, 
    BatchLogger batchLogger,
    TimeSpan batchDelay = default)
{
    private readonly IProgressListener progressListener = progressListener;
    private readonly TimeSpan defaultBatchDelay = batchDelay;

    [Obsolete("Please use ProcessBatches.")]
    public async Task ProcessAllBatchesAsync<TInput, TOutput>(
        IBatchIntegrationStep<TInput, TOutput> step, string stepName)
    {
        await ProcessBatchesAsync(step, stepName);
    }

    public async Task ProcessBatchesAsync<TInput, TOutput>(
        IBatchIntegrationStep<TInput, TOutput> step, string stepName, int? maxBatchCount = null)
    {
        step.OnStepStart();

        BatchState<TInput>? batchState = null;
        TimeSpan batchDelay = step.BatchDelay ?? defaultBatchDelay;
        int batchCount = 0;

        do
        {
            if (batchState is not null && batchDelay.Ticks > 0) await Task.Delay(batchDelay);
            batchState = await ProcessBatchAsync(step, stepName, batchState, maxBatchCount);
            batchCount++;

            if (maxBatchCount is not null && batchCount == maxBatchCount)
            {
                break;
            }
        }
        while (batchState.QueryState?.MoreRecords == true);

        step.OnStepFinished();
    }

    public async Task<BatchState<TInput>> ProcessBatchAsync<TInput, TOutput>(
        IBatchIntegrationStep<TInput, TOutput> step,
        string stepName,
        BatchState<TInput>? batchState,
        int? maxBatchCount = null)
    {

        var queryState = batchState?.QueryState;
        var progress = batchState?.Progress;

        // initialize state if needed
        var query = step.GetInputQuery();
        if (queryState is null)
        {
            var total = await query.GetTotalRecordCountAsync(step.BatchSize, maxBatchCount);
            queryState = new(0, step.BatchSize, 0, total, true, null);
        }

        progress ??= new IntegrationProgress(stepName, queryState.TotalRecordCount);

        // get batch of data (IPagedQuery)
        var result = await query.GetNextPageAsync(queryState.NextPageNumber, queryState.RecordsPerPage, queryState.Bookmark);
        var batch = result.Records;
        var proccessedCount = batch.Count;

        batch = await step.OnBeforeMapAsync(batch);

        IReadOnlyList<DataOperation<TOutput>> data = new List<DataOperation<TOutput>>(batch.SelectMany(step.MapRecord));

        data = await step.OnAfterMapAsync(data);

        data = await step.OnBeforeDeliveryAsync(data);

        var results = await step.OutputDataSource.PerformOperationsAsync(data, step.DryRun, step.DataOperationFlags);

        batchLogger.LogFailures(results, step.KeySelector);

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


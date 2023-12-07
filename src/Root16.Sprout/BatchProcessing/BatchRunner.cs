using Root16.Sprout.DataSources;
using Root16.Sprout.Progress;

namespace Root16.Sprout.BatchProcessing;

public class BatchRunner
{
    public BatchRunner(IProgressListener progressListener)
    {
        this.progressListener = progressListener;
    }

    private readonly IProgressListener progressListener;

    public async Task<PagedQueryState<TInput>> ProcessBatchAsync<TInput, TOutput>(
        IBatchIntegrationStep<TInput,TOutput> step,
        PagedQueryState<TInput>? queryState,
        int? pageSize = null)
    {
        // initialize state if needed
        var query = step.GetSourceQuery();
        if (queryState == null)
        {
            var total = await query.GetTotalRecordCountAsync();
            queryState = new(true, 0, total, pageSize ?? 200, null);
        }

        // get batch of data (IPagedQuery)
        pageSize ??= 200;
        var progress = new IntegrationProgress(step.GetType().Name, queryState.TotalRecordCount);

        // TODO: pass in query state
        var result = await query.GetNextPageAsync(queryState.NextPageNumber, queryState.RecordsPerPage, queryState.Bookmark);
        var batch = result.Records;
        var proccessedCount = batch.Count;

        batch = await step.OnBeforeMapAsync(batch);

        IReadOnlyList<DataOperation<TOutput>> data = new List<DataOperation<TOutput>>(batch.SelectMany(step.MapRecord));

        data = await step.OnAfterMapAsync(data);

        data = await step.OnBeforeDeliveryAsync(data);
        var results = await step.OutputDataSource.PerformOperationsAsync(data);
        await step.OnAfterDeliveryAsync(results);

        // report progress (IProgressListener)
        progress.AddOperations(proccessedCount, results.Select(r => r.WasSuccessful ? "Error" : r.Operation.OperationType));
        progressListener.OnProgressChange(progress);


        // return state (more records, paging details) from IPagedQuery

        return new PagedQueryState<TInput>(
            result.MoreRecords,
            queryState.NextPageNumber + 1,
            queryState.TotalRecordCount,
            queryState.RecordsPerPage,
            queryState.Bookmark);
    }
}


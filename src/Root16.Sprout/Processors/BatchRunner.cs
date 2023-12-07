using Microsoft.Extensions.DependencyInjection;
using Root16.Sprout.Data;
using Root16.Sprout.Progress;
using Root16.Sprout.Query;
using Root16.Sprout.Step;

namespace Root16.Sprout.Processors;

public class BatchRunner
{
    public BatchRunner(IProgressListener progressListener)
    {
        this.progressListener = progressListener;
    }

    private readonly IProgressListener progressListener;

    public async Task<PagedQueryState> ProcessBatchAsync<TInput, TOutput>(
        IBatchIntegrationStep<TInput,TOutput> step,
        PagedQueryState? queryState,
        int? pageSize = null)
    {
        // initialize state if needed
        var query = step.GetSourceQuery();
        if (queryState == null)
        {
            queryState = new();
            queryState.TotalRecordCount =query.GetTotalRecordCount();
        }

        // get batch of data (IPagedQuery)
        pageSize ??= 200;
        var progress = new IntegrationProgress(step.GetType().Name, queryState.TotalRecordCount);

        // TODO: pass in query state
        var batch = query.GetNextPage(pageSize.Value);
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

        return queryState;
    }
}

public record DataOperation<T>(string OperationType, T Data);
public record DataOperationResult<T>(DataOperation<T> Operation, bool WasSuccessful);

public interface IDataProcessor<TInput, TOutput>
{
    TOutput Process(TInput record);
}

public interface IDataProcessor<T>
{
    Task<IEnumerable<T>> ProcessBatchAsync(IEnumerable<T> input);
}

public class DelegateDataProcessor<T> : IDataProcessor<T>
{
    private readonly Func<IEnumerable<T>, IEnumerable<T>> processor;

    public DelegateDataProcessor(Func<IEnumerable<T>,IEnumerable<T>> processor)
    {
        this.processor = processor;
    }

    public Task<IEnumerable<T>> ProcessBatchAsync(IEnumerable<T> input)
    {
        return Task.FromResult(processor(input));
    }
}

public class AsyncDelegateDataProcessor<T> : IDataProcessor<T>
{
    private readonly Func<IEnumerable<T>, Task<IEnumerable<T>>> processor;

    public AsyncDelegateDataProcessor(Func<IEnumerable<T>, Task<IEnumerable<T>>> processor)
    {
        this.processor = processor;
    }

    public async Task<IEnumerable<T>> ProcessBatchAsync(IEnumerable<T> input)
    {
        return await processor(input);
    }
}

public interface IDataRepository<T>
{
    Task PerformOperationsAsync(IEnumerable<DataOperation<T>> operations);
}

public class PagedQueryState
{
    public int? TotalRecordCount { get; set; }
    public bool MoreRecords { get; set; }
    public object? State { get; set; }
}


using Microsoft.Extensions.DependencyInjection;
using Root16.Sprout.Data;
using Root16.Sprout.Progress;
using Root16.Sprout.Query;

namespace Root16.Sprout.Processors;

public interface IBatchProcessorBuilder<TInput, TOutput>
{
    IBatchProcessorBuilder<TInput, TOutput> AddPreMappingProcessor(Func<IEnumerable<TInput>, IEnumerable<TInput>> processor);
    IBatchProcessorBuilder<TInput, TOutput> AddPostMappingProcessor(Func<IEnumerable<DataOperation<TOutput>>, IEnumerable<DataOperation<TOutput>>> processor);
    BatchProcessor<TInput, TOutput> Build();
    IBatchProcessorBuilder<TInput, TOutput> UseMapper(Func<TInput, IEnumerable<DataOperation<TOutput>>> mapper);
    IBatchProcessorBuilder<TInput, TOutput> UseDataOperationEndpoint(IDataOperationEndpoint<TOutput> dataOperationEndpoint);
}

public class BatchProcessBuilder
{
    private readonly IServiceProvider serviceProvider;
    

    public BatchProcessBuilder(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public IBatchProcessorBuilder<TInput,TOutput> CreateProcessor<TInput,TOutput>(IPagedQuery<TInput> query)
    {
        return new InternalBatchProcessorBuilder<TInput, TOutput>(serviceProvider, query);
    }

    private class InternalBatchProcessorBuilder<TInput,TOutput> : IBatchProcessorBuilder<TInput,TOutput>
    {
        BatchProcessor<TInput,TOutput> processor;

        public InternalBatchProcessorBuilder(IServiceProvider serviceProvider, IPagedQuery<TInput> query)
        {
            processor = serviceProvider.GetRequiredService<BatchProcessor<TInput, TOutput>>();
            processor.SourceQuery = query;
        }

        public IBatchProcessorBuilder<TInput, TOutput> AddPreMappingProcessor(Func<IEnumerable<TInput>,IEnumerable<TInput>> proc)
        {
            processor.PreMappingProcessors.Add(new DelegateDataProcessor<TInput>(proc));
            return this;
        }

        public IBatchProcessorBuilder<TInput,TOutput> UseMapper(Func<TInput, IEnumerable<DataOperation<TOutput>>> mapper)
        {
            processor.Mapper = mapper;
            return this;   
        }

        public IBatchProcessorBuilder<TInput,TOutput> UseDataOperationEndpoint(IDataOperationEndpoint<TOutput> dataOperationEndpoint)
        {
            processor.DataOperationEndpoint = dataOperationEndpoint;
            return this;
        }

        public IBatchProcessorBuilder<TInput, TOutput> AddPostMappingProcessor(Func<IEnumerable<DataOperation<TOutput>>, IEnumerable<DataOperation<TOutput>>> proc)
        {
            processor.PostMappingProcessors.Add(new DelegateDataProcessor<DataOperation<TOutput>>(proc));
            return this;
        }

        public BatchProcessor<TInput, TOutput> Build()
        {
            processor.ValidateConfiguration();
            return processor;
        }
    }
}

public class BatchProcessor<TInput, TOutput>
{
    public BatchProcessor(IProgressListener progressListener)
    {
        this.progressListener = progressListener;
    }

    public IList<IDataProcessor<TInput>> PreMappingProcessors { get; } = new List<IDataProcessor<TInput>>();
    public IList<IDataProcessor<DataOperation<TOutput>>> PostMappingProcessors { get; } = new List<IDataProcessor<DataOperation<TOutput>>>();
    
    
    private readonly IProgressListener progressListener;

    public IPagedQuery<TInput>? SourceQuery { get; set; }
    public Func<TInput, IEnumerable<DataOperation<TOutput>>>? Mapper { get; set; }
    public IDataOperationEndpoint<TOutput>? DataOperationEndpoint { get; set; }

    
    public async Task<PagedQueryState> ProcessBatchAsync(
        PagedQueryState? queryState,
        int? pageSize = null)
    {
        ValidateConfiguration();

        // initialize state if needed
        if (queryState == null)
        {
            queryState = new();
            queryState.TotalRecordCount = SourceQuery!.GetTotalRecordCount();
        }

        // get batch of data (IPagedQuery)
        pageSize ??= 200;
        var batch = SourceQuery!.GetNextPage(pageSize.Value);

        // map data (IDataMapper?)
        var results = new List<DataOperation<TOutput>>();

        // match to existing data (IDataRepository)
        foreach (var processor in PreMappingProcessors)
        {
            batch = new List<TInput>(await processor.ProcessBatchAsync(batch));
        }

        foreach (var record in batch)
        {
            results.AddRange(Mapper!(record));
        }

        // filter out stuff (identical records) (IDataFilter[]) how to pass state (new data, existing data)?
        foreach (var processor in PostMappingProcessors)
        {
            results = new List<DataOperation<TOutput>>(await processor.ProcessBatchAsync(results));
        }

        await DataOperationEndpoint!.PerformOperationsAsync(results);

        // send to data sink (IDataRepository)

        // report progress (IProgressListener)
        // progressListener.OnProgressChange();


        // return state (more records, paging details) from IPagedQuery

        return queryState;
    }

    public void ValidateConfiguration()
    {
        if (SourceQuery == null)
        {
            throw new InvalidOperationException($"{nameof(SourceQuery)} must be defined.");
        }

        if (Mapper == null)
        {
            throw new InvalidOperationException($"{nameof(Mapper)} must be defined.");
        }

        if (DataOperationEndpoint == null)
        {
            throw new InvalidOperationException($"{nameof(DataOperationEndpoint)} must be defined.");
        }
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


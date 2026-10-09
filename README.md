# sprout

Sprout is a .NET library for building data integrations and migrations between systems. It was originally built to support high-throughput integration with Microsoft Dataverse, which it typically uses as the source or destination of an integration, but it is not limited to Dataverse — CSV, Excel, and SQL Server data sources are supported out of the box, and custom sources are easy to add.

Sprout gives you:

- **Steps** — units of work (`IIntegrationStep`) that read from an input, map records, and deliver them to an output.
- **Batch processing** — steps built on `BatchIntegrationStep<TInput, TOutput>` page through input in batches, map each record to one or more data operations, and deliver them with configurable batch size, delay, and dry-run support.
- **A runtime** — `IIntegrationRuntime` resolves registered steps, runs a single step by name/type, or runs all steps together, honoring prerequisite dependencies and an optional degree of parallelism.
- **Data sources** — a common `IDataSource<T>` / `IPagedQuery<T>` abstraction, with implementations for in-memory data, Dataverse, SQL Server, CSV, and Excel.

## Packages

| Package | Description |
|---|---|
| `Root16.Sprout` | Core abstractions: integration steps, the runtime, batch processing, and an in-memory data source. |
| `Root16.Sprout.Dataverse` | A `DataverseDataSource` for reading/writing Dataverse records, plus entity operation reduction and batch analysis helpers. |
| `Root16.Sprout.SqlServer` | A SQL Server data source with paged and reducing queries. |
| `Root16.Sprout.CSV` | A CSV data source built on CsvHelper, with class-map support. |
| `Root16.Sprout.Excel` | An Excel data source built on ExcelDataReader, with pluggable row mapping via `IExcelMapper<T>`. |

Each package targets `net6.0` through `net10.0` and is published to NuGet.

## Getting started

Register Sprout and the data sources you need with your `IServiceCollection`, register one or more steps, then run them through `IIntegrationRuntime`:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSprout();
builder.Services.AddSproutDataverse();
builder.Services.AddDataverseDataSource("Dataverse");

builder.Services.AddSingleton(
    _ => new MemoryDataSource<CreateContact>(
    [
        new CreateContact { FirstName = "Ada", LastName = "Lovelace" },
        new CreateContact { FirstName = "Alan", LastName = "Turing" },
    ])
);

builder.Services.RegisterStep<CreateContactTestStep>();

var host = builder.Build();
host.Start();

var runtime = host.Services.GetRequiredService<IIntegrationRuntime>();
await runtime.RunStepAsync<CreateContactTestStep>();
// or: await runtime.RunAllStepsAsync(maxDegreesOfParallelism: 4);
```

### Writing a step

Most steps derive from `BatchIntegrationStep<TInput, TOutput>`, which pages through an input query, maps each record to one or more `DataOperation<TOutput>`s, and delivers them to an `IDataSource<TOutput>` in batches:

```csharp
internal class CreateContactTestStep : BatchIntegrationStep<CreateContact, Entity>
{
    private readonly DataverseDataSource dataverseDataSource;
    private readonly BatchProcessor batchProcessor;
    private readonly MemoryDataSource<CreateContact> memoryDS;

    public CreateContactTestStep(MemoryDataSource<CreateContact> memoryDS, DataverseDataSource dataverseDataSource, BatchProcessor batchProcessor)
    {
        this.memoryDS = memoryDS;
        this.dataverseDataSource = dataverseDataSource;
        this.batchProcessor = batchProcessor;
        BatchSize = 200;
    }

    public override IDataSource<Entity> OutputDataSource => dataverseDataSource;

    public override IPagedQuery<CreateContact> GetInputQuery() => memoryDS.CreatePagedQuery();

    public override IReadOnlyList<DataOperation<Entity>> MapRecord(CreateContact source)
    {
        var entity = new Entity("contact")
        {
            Attributes =
            {
                { "firstname", source.FirstName },
                { "lastname", source.LastName },
            }
        };

        return [new DataOperation<Entity>("Create", entity)];
    }

    public override async Task RunAsync(string stepName)
    {
        await batchProcessor.ProcessBatchesAsync(this, stepName);
    }
}
```

Steps can hook into the batch lifecycle (`OnBeforeMapAsync`, `OnAfterMapAsync`, `OnBeforeDeliveryAsync`, `OnAfterDeliveryAsync`) to do things like duplicate detection, deduplication of operations within a batch, or custom logging/error handling. Declare prerequisites when registering a step so the runtime sequences dependent steps correctly:

```csharp
builder.Services.RegisterStep<UpdateContactTestStep>(nameof(CreateContactTestStep));
```

### Reusing a step with different configuration

The same step type can be registered more than once under different names, each with its own configuration object passed to the constructor. This is useful when one mapping applies to several source files, tables, or environments:

```csharp
public record ImportOptions(string TableName, string FilePath);

internal class ImportStep : BatchIntegrationStep<DataRow, Entity>
{
    public ImportStep(ImportOptions options, DataverseDataSource dataverse, BatchProcessor batchProcessor) { /* … */ }
    // …
}

builder.Services.RegisterStepWithArgument<ImportStep, ImportOptions>(
    "ImportAccounts", new ImportOptions("account", "data/accounts.csv"));

builder.Services.RegisterStepWithArgument<ImportStep, ImportOptions>(
    "ImportContacts", new ImportOptions("contact", "data/contacts.csv"),
    "ImportAccounts"); // prerequisite step name(s)

await runtime.RunStepAsync("ImportContacts");
```

`RegisterStepWithArguments` (plural) accepts an ordered list of constructor arguments instead of a single configuration object, and the `RegisterStep` overloads that take a `Func<IServiceProvider, object?, TStep>` let you construct the step yourself.

## Samples

The `src` folder includes runnable samples demonstrating each scenario:

- `Root16.Sprout.Sample.CreatesAndUpdates` — creating, updating, and deleting Dataverse records, including operation reduction and error reporting.
- `Root16.Sprout.Sample.ParallelSteps` — running independent steps concurrently via `RunAllStepsAsync`.
- `Root16.Sprout.Sample.SqlServer` — reading from SQL Server, including file-based and reducing queries.
- `Root16.Sprout.CSV.Sample` — importing data from a CSV file.
- `Root16.Sprout.Excel.Sample` — importing data from an Excel workbook.

## Building

```bash
dotnet build src/Root16.Sprout.sln
```

Unit tests live in `Root16.Sprout.UnitTests`:

```bash
dotnet test src/Root16.Sprout.UnitTests/Root16.Sprout.UnitTests.csproj
```

## License

MIT — see [LICENSE](LICENSE).

using Root16.Sprout.BatchProcessing;
using Root16.Sprout.CSV.Factories;
using Root16.Sprout.CSV.Sample.Models;
using Root16.Sprout.DataSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Root16.Sprout.CSV.Sample.Steps;

public class ExampleCSVStep1(BatchProcessor batcher, ICSVDataSourceFactory csvDataSourceFactory) : BatchIntegrationStep<TestClass1, TestClass1>
{
    private readonly BatchProcessor _batcher = batcher;
    private readonly CSVDataSource<TestClass1> _csvDataSource = csvDataSourceFactory.GetCSVDataSourceByName<TestClass1>("CSV1");
    private readonly MemoryDataSource<TestClass1> _memoryDS = new();

    public override IDataSource<TestClass1> OutputDataSource => _memoryDS;

    public override IPagedQuery<TestClass1> GetInputQuery()
    {
        return _csvDataSource.CreatePagedQuery();
    }

    public override IReadOnlyList<DataOperation<TestClass1>> MapRecord(TestClass1 source)
    {
        return [new DataOperation<TestClass1>("Create", source)];
    }

    public override Task RunAsync(string stepName)
    {
        BatchSize = 75000;
        return _batcher.ProcessAllBatchesAsync(this, stepName);
    }
}
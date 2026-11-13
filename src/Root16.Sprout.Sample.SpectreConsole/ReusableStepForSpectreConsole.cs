using Root16.Sprout.BatchProcessing;
using Root16.Sprout.DataSources;
using Root16.Sprout.Sample.SpectreConsole.Models;

namespace Root16.Sprout.Sample.SpectreConsole
{
    public class ReusableStepForSpectreConsole : BatchIntegrationStep<SampleData, SampleData>
    {
        private readonly MemoryDataSource<SampleData> outputDataSource = new();
        private readonly MemoryDataSource<SampleData> InputDataSource = new();
        private readonly BatchProcessor processor;


        public ReusableStepForSpectreConsole(BatchProcessor batchProcessor)
        {
            BatchSize = 5000;
            processor = batchProcessor;
            for (int i = 0; i < Random.Shared.Next(10000, 60000); i++)
            {
                InputDataSource.Records.Add(new SampleData());
            }
        }

        public override IDataSource<SampleData> OutputDataSource => outputDataSource;

        public override IPagedQuery<SampleData> GetInputQuery()
        {
            return InputDataSource.CreatePagedQuery();
        }

        public override IReadOnlyList<DataOperation<SampleData>> MapRecord(SampleData source)
        {
            return [];
        }

        public override async Task RunAsync(string stepName)
        {
            await processor.ProcessBatchesAsync(this, stepName);
        }
    }
}

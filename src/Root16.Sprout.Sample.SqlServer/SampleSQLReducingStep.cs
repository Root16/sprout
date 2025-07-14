using Microsoft.Data.SqlClient;
using Root16.Sprout.BatchProcessing;
using Root16.Sprout.DataSources;
using Root16.Sprout.DataSources.Sql;
using Root16.Sprout.Extensions;
using System.Data;
using System.Diagnostics;

namespace Root16.Sprout.Sample.SqlServer
{
    public class SampleSQLReducingStep : BatchIntegrationStep<DataRow, IDbCommand>
    {
        private readonly SqlDataSource _sqlDataSource;
        private readonly BatchProcessor _batchProcessor;

        public SampleSQLReducingStep(BatchProcessor batchProcessor, SqlDataSource sqlDataSource)
        {
            _batchProcessor = batchProcessor;
            _sqlDataSource = sqlDataSource;
            BatchSize = 5;
            DryRun = true;
        }

        public override IDataSource<IDbCommand> OutputDataSource => _sqlDataSource;

        public override IPagedQuery<DataRow> GetInputQuery()
        {
            return _sqlDataSource.CreateReducingQuery("SELECT [Id] FROM [master].[dbo].[MorePersons] WHERE Address = 'Tester'",
            "SELECT COUNT(Id) as count FROM [master].[dbo].[MorePersons] WHERE Address = 'Tester'");
        }

        public override IReadOnlyList<DataOperation<IDbCommand>> MapRecord(DataRow source)
        {
            List<DataOperation<IDbCommand>> operations = [];
            var command = new SqlCommand
            {
                CommandText = "UPDATE [master].[dbo].[MorePersons] SET [Address] = @Address WHERE  Id = @Id",
                CommandType = CommandType.Text
            };

            command.Parameters.Add(new SqlParameter("@Address", "AddressTesting"));
            command.Parameters.Add(new SqlParameter("@Id", source.GetValue<int>("Id")));

            operations.Add(new DataOperation<IDbCommand>("Insert", command));

            return operations;
        }

        public override async Task RunAsync(string stepName)
        {
            await _batchProcessor.ProcessBatchesAsync(this, stepName);
        }
    }
}

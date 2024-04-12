using Microsoft.Data.SqlClient;
using Root16.Sprout.BatchProcessing;
using Root16.Sprout.DataSources;
using Root16.Sprout.DataSources.Sql;
using System.Data;

namespace Root16.Sprout.Sample.SqlServer
{
    public class SampleSQLStep : BatchIntegrationStep<DataRow, IDbCommand>
    {
        private readonly SqlDataSource _sqlDataSource;
        private readonly BatchProcessor _batchProcessor;

        public SampleSQLStep(BatchProcessor batchProcessor, SqlDataSource sqlDataSource)
        {
            _batchProcessor = batchProcessor;
            _sqlDataSource = sqlDataSource;
            BatchSize = 50;
        }

        public override IDataSource<IDbCommand> OutputDataSource => _sqlDataSource;

        //Todo: Add 
        public override IPagedQuery<DataRow> GetInputQuery()
        {
            return _sqlDataSource.CreatePagedQuery("SELECT [PersonID],[LastName],[FirstName],[Address],[City] FROM [master].[dbo].[Persons]");
        }

        public override IReadOnlyList<DataOperation<IDbCommand>> MapRecord(DataRow source)
        {
            List<DataOperation<IDbCommand>> operations = [];

            for (int i = 0; i < 25; i++)
            {
                var command = new SqlCommand
                {
                    CommandText = "INSERT INTO [dbo].[Persons] ([PersonID],[LastName],[FirstName],[Address],[City]) VALUES (@PersonId,@LastName,@FirstName,@Address,@City)",
                    CommandType = CommandType.Text
                };
                command.Parameters.Add(new SqlParameter("@PersonId", 5));
                command.Parameters.Add(new SqlParameter("@LastName", "Tester"));
                command.Parameters.Add(new SqlParameter("@FirstName", "Tester"));
                command.Parameters.Add(new SqlParameter("@Address", "Tester"));
                command.Parameters.Add(new SqlParameter("@City", "Tester"));

                operations.Add(new DataOperation<IDbCommand>("Insert", command));
            }

            return operations;
        }

        public override async Task RunAsync()
        {
            await _batchProcessor.ProcessAllBatchesAsync(this);
        }
    }
}

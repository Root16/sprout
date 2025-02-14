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
            BatchSize = 1;
        }

        public override IDataSource<IDbCommand> OutputDataSource => _sqlDataSource;

        //Todo: Add 
        public override IPagedQuery<DataRow> GetInputQuery()
        {
            return _sqlDataSource.CreatePagedQuery("SELECT [PersonID],[LastName],[FirstName],[Address],[City] FROM [master].[dbo].[Persons]");
        }
        public override async Task<IReadOnlyList<DataRow>> OnBeforeMapAsync(IReadOnlyList<DataRow> batch)
		{
            var standardQuery = await _sqlDataSource.ExecuteQueryAsync("SELECT TOP 20 [PersonID],[LastName],[FirstName],[Address],[City] FROM [master].[dbo].[Persons]");

            var pagedQuery = await _sqlDataSource.ExecuteQueryWithPagingAsync("SELECT [PersonID],[LastName],[FirstName],[Address],[City] FROM [master].[dbo].[Persons]");

            var pagedQueryWithBatchSize = await _sqlDataSource.ExecuteQueryWithPagingAsync("SELECT [PersonID],[LastName],[FirstName],[Address],[City] FROM [master].[dbo].[Persons]", batchSize: 3);

            return await base.OnBeforeMapAsync(batch);
		}
        public override IReadOnlyList<DataOperation<IDbCommand>> MapRecord(DataRow source)
        {
            List<DataOperation<IDbCommand>> operations = [];

            for (int i = 0; i < 2; i++)
            {
                var command = new SqlCommand
                {
                    CommandText = "INSERT INTO [dbo].[MorePersons] ([PersonID],[LastName],[FirstName],[Address],[City]) VALUES (@PersonId,@LastName,@FirstName,@Address,@City)",
                    CommandType = CommandType.Text
                };
                command.Parameters.Add(new SqlParameter("@PersonId", 9));
                command.Parameters.Add(new SqlParameter("@LastName", "Tester"));
                command.Parameters.Add(new SqlParameter("@FirstName", "Tester"));
                command.Parameters.Add(new SqlParameter("@Address", "Tester"));
                command.Parameters.Add(new SqlParameter("@City", "Tester"));

                operations.Add(new DataOperation<IDbCommand>("Insert", command));
            }

            return operations;
        }

        public override async Task RunAsync(string stepName)
        {
            await _batchProcessor.ProcessAllBatchesAsync(this, stepName);
        }
    }
}

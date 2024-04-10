using Microsoft.Data.SqlClient;
using Microsoft.Xrm.Sdk;
using Root16.Sprout.BatchProcessing;
using Root16.Sprout.DataSources;
using Root16.Sprout.DataSources.Dataverse;
using System.Data;

namespace Root16.Sprout.Sample.SqlServer
{
    public class SampleSQLStep : BatchIntegrationStep<DataRow, IDbCommand>
    {
        private readonly DataverseDataSource? _targetDataverseDataSource;
        private readonly EntityOperationReducer _reducer;
        private readonly SqlDataSource _sqlDataSource;
        private readonly BatchProcessor _batchProcessor;

        public SampleSQLStep(IDataverseDataSourceFactory factory, BatchProcessor batchProcessor, EntityOperationReducer reducer, SqlDataSource sqlDataSource)
        {
            _targetDataverseDataSource = null;
            _batchProcessor = batchProcessor;
            _reducer = reducer;
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
            List<DataOperation<IDbCommand>> operations = new();

            for (int i = 0; i < 25; i++)
            {
                var command = new SqlCommand
                {
                    CommandText = "INSERT INTO [dbo].[Persons] ([PersonID],[LastName],[FirstName],[Address],[City]) VALUES (@PersonId,@LastName,@FirstName,@Address,@City)",
                    CommandType = CommandType.Text
                };
                command.Parameters.Add(new SqlParameter("@PersonId", "ItsATest"));
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

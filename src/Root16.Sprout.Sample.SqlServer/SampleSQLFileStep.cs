using Microsoft.Data.SqlClient;
using Root16.Sprout.BatchProcessing;
using Root16.Sprout.DataSources;
using Root16.Sprout.DataSources.Sql;
using System.Data;

namespace Root16.Sprout.Sample.SqlServer
{
    public class SampleSQLFileStep : BatchIntegrationStep<DataRow, IDbCommand>
    {
        private readonly SqlDataSource _sqlDataSource;
        private readonly BatchProcessor _batchProcessor;

        public SampleSQLFileStep(BatchProcessor batchProcessor, SqlDataSource sqlDataSource)
        {
            _batchProcessor = batchProcessor;
            _sqlDataSource = sqlDataSource;
            BatchSize = 1;
        }

        public override IDataSource<IDbCommand> OutputDataSource => _sqlDataSource;

        //Todo: Add 
        public override IPagedQuery<DataRow> GetInputQuery()
        {
            return _sqlDataSource.CreatePagedQueryFromFile(@"..\..\..\SQLFiles\query.sql", @"SELECT count(*) as Count FROM [master].[dbo].[Persons];");
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
                command.Parameters.Add(new SqlParameter("@PersonId", 12));
                command.Parameters.Add(new SqlParameter("@LastName", "Tester"));
                command.Parameters.Add(new SqlParameter("@FirstName", "Tester"));
                command.Parameters.Add(new SqlParameter("@Address", "Tester"));
                command.Parameters.Add(new SqlParameter("@City", "SingularFile"));

                operations.Add(new DataOperation<IDbCommand>("Insert", command));
            }

            return operations;
        }

        public override async Task RunAsync(string stepName)
        {
            await _batchProcessor.ProcessBatchesAsync(this, stepName);
        }
    }
}

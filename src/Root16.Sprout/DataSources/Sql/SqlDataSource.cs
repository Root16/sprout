using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Root16.Sprout.DataSources.Dataverse;

public class SqlDataSource : IDataSource<DataRow>
{
    private readonly string connectionString;
    private readonly ILogger<SqlDataSource> logger;
    private readonly SqlConnection connection;

    public SqlDataSource(string connectionString, ILogger<SqlDataSource> logger)
    {
        this.connectionString = connectionString;
        this.logger = logger;
        connection = new SqlConnection(connectionString);
    }

    public SqlPagedQuery CreatePagedQuery(string commandText, string totalRowCountCommandText = null, bool addPaging = true)
    {
        return new SqlPagedQuery(connection, commandText, totalRowCountCommandText, addPaging);
    }

    public DynamicSqlPagedQuery CreatePagedQuery(Func<int, int, string> commandGenerator, string totalRowCountCommandText = null)
    {
        return new DynamicSqlPagedQuery(connection, commandGenerator, totalRowCountCommandText);
    }

    public void ExecuteNonQuery(string commandText)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = commandText;
            command.Connection.Open();
            try
            {
                command.ExecuteNonQuery();
            }
            finally
            {
                command.Connection.Close();
            }
        }
    }

    public Task<IReadOnlyList<DataOperationResult<DataRow>>> PerformOperationsAsync(IEnumerable<DataOperation<DataRow>> operations)
    {
        throw new NotImplementedException();
    }
}

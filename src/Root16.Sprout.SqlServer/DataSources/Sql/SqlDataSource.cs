using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Root16.Sprout.DataSources.Sql;

public class SqlDataSource(string connectionString, ILogger<SqlDataSource> logger) : IDataSource<DataRow>, IDataSource<IDbCommand>
{
    private readonly ILogger<SqlDataSource> logger = logger;
    public readonly SqlConnection connection = new(connectionString);

    public SqlPagedQuery CreatePagedQuery(string commandText, string? totalRowCountCommandText = null, bool addPaging = true)
    {
        return new SqlPagedQuery(connection, commandText, totalRowCountCommandText, addPaging);
    }

    public DynamicSqlPagedQuery CreatePagedQuery(Func<int, int, string> commandGenerator, string? totalRowCountCommandText = null)
    {
        return new DynamicSqlPagedQuery(connection, commandGenerator, totalRowCountCommandText);
    }

    public void ExecuteNonQuery(string commandText)
    {
        using var command = connection.CreateCommand();
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

    public async Task<IReadOnlyList<DataOperationResult<IDbCommand>>> PerformOperationsAsync(IEnumerable<DataOperation<IDbCommand>> operations, bool dryRun, IEnumerable<string> dataOperationFlags)
    {
        if (dryRun)
        {
            return operations.Select(x => new DataOperationResult<IDbCommand>(x, true)).ToList();
        }

        var finishedOperations = new List<DataOperationResult<IDbCommand>>();
        foreach (var dataOperation in operations)
        {
            var command = dataOperation.Data;
            command.Connection = connection;
            command.Connection.Open();
            try
            {
                command.ExecuteNonQuery();
                finishedOperations.Add(new DataOperationResult<IDbCommand>(dataOperation, true));
            } catch(Exception ex)
            {
                logger.LogError(ex.Message);
                finishedOperations.Add(new DataOperationResult<IDbCommand>(dataOperation, false));
            }
            finally
            {
                command.Connection.Close();
            }
        }

        return finishedOperations;
    }

    public Task<IReadOnlyList<DataOperationResult<DataRow>>> PerformOperationsAsync(IEnumerable<DataOperation<DataRow>> operations, bool dryRun, IEnumerable<string> dataOperationFlags)
    {
        throw new NotImplementedException();
    }
}

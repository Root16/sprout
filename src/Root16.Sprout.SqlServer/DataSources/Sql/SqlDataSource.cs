using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Root16.Sprout.DataSources.Sql;

public class SqlDataSource(string connectionString, ILoggerFactory loggerFactory) : IDataSource<DataRow>, IDataSource<IDbCommand>
{
    private readonly ILoggerFactory loggerFactory = loggerFactory;
    private readonly ILogger<SqlDataSource> logger = loggerFactory.CreateLogger<SqlDataSource>();
    public readonly SqlConnection connection = new(connectionString);

    public SqlPagedQuery CreatePagedQuery(string commandText, string? totalRowCountCommandText = null, bool addPaging = true)
    {
        return new SqlPagedQuery(loggerFactory.CreateLogger<SqlPagedQuery>(), connection, commandText, totalRowCountCommandText, addPaging);
    }

    public DynamicSqlPagedQuery CreatePagedQuery(Func<int, int, string> commandGenerator, string? totalRowCountCommandText = null)
    {
        return new DynamicSqlPagedQuery(loggerFactory.CreateLogger<DynamicSqlPagedQuery>(), connection, commandGenerator, totalRowCountCommandText);
    }

    public SqlPagedQuery CreatePagedQueryFromFile(string commandFilePath, string? totalRowCountCommandText = null, bool addPaging = true)
    {
        using StreamReader reader = new(commandFilePath.ToString());
        var commandText = reader.ReadToEnd();
        return new SqlPagedQuery(loggerFactory.CreateLogger<SqlPagedQuery>(), connection, commandText, totalRowCountCommandText, addPaging);
    }

    public SqlPagedQuery CreatePagedQueryFromFiles(string commandFilePath, string totalRowCommandTextFilePath, bool addPaging = true)
    {
        using StreamReader commandReader = new(commandFilePath);
        using StreamReader totalRowCommandReader = new(totalRowCommandTextFilePath);
        var command = commandReader.ReadToEnd();
        var totalCommand = totalRowCommandReader.ReadToEnd();
        return new SqlPagedQuery(loggerFactory.CreateLogger<SqlPagedQuery>(), connection, command, totalCommand, addPaging);
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
    public async Task<IReadOnlyList<DataRow>> ExecuteQueryAsync(string commandText)
	{
		var records = new List<DataRow>();

		var query = new SqlPagedQuery(
            loggerFactory.CreateLogger<SqlPagedQuery>(), 
            connection, 
            commandText, 
			null, //not relevant for this method
            addPaging: false);

        var result = await query.GetNextPageAsync(
                0,
                -1, //Not needed when addPaging was set to false
				null);

		var batch = result.Records;

	    records.AddRange(batch);

		return records;
	}
	public async Task<IReadOnlyList<DataRow>> ExecuteQueryWithPagingAsync(string commandText, int batchSize = 200)
	{
		var records = new List<DataRow>();

		var query = new SqlPagedQuery(
            loggerFactory.CreateLogger<SqlPagedQuery>(), 
            connection, 
            commandText, 
			null, //not relevant for this method
            addPaging: true);

		PagedQueryState<DataRow> queryState = new(
            0, 
            batchSize, 
            0, 
			null, //not relevant for this method
            true, 
            null);

		while (queryState.MoreRecords)
		{
			var result = await query.GetNextPageAsync(
				queryState.NextPageNumber,
				queryState.RecordsPerPage,
				queryState.Bookmark);

			var batch = result.Records;

			var proccessedCount = batch.Count;

			queryState = new
			(
				queryState.NextPageNumber + 1,
				queryState.RecordsPerPage,
				proccessedCount,
				null, //Not relevant for this method
				result.MoreRecords,
				queryState.Bookmark
			);

			records.AddRange(batch);
		}

		return records;
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
				if (logger.IsEnabled(LogLevel.Debug))
				{
					logger.LogError(ex, ex.Message);
				}
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

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Root16.Sprout.DataSources.Sql;

public class SqlPagedQuery(ILogger<SqlPagedQuery> logger, SqlConnection connection, string commandText, string? totalRowCountCommandText = null, bool addPaging = true) : IPagedQuery<DataRow>
{
    private readonly ILogger<SqlPagedQuery> logger = logger;
    private readonly SqlConnection connection = connection;
    private readonly string commandText = commandText;
    private readonly string? totalRowCountCommandText = totalRowCountCommandText;
    private readonly bool addPaging = addPaging;

    const int MaxRetries = 10;

    public async Task<PagedQueryResult<DataRow>> GetNextPageAsync(int pageNumber, int pageSize, object? bookmark)
    {
        using var command = connection.CreateCommand();
        command.CommandText = commandText;
        if (addPaging)
        {
            if (commandText.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
            {
                command.CommandText += $" OFFSET {pageNumber * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            }
            else
            {
                command.CommandText += $" ORDER BY (SELECT NULL) OFFSET {pageNumber * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            }
        }
        command.Connection.Open();
        var reader = await TryAsync(() => command.ExecuteReaderAsync(CommandBehavior.CloseConnection));
        try
        {
            DataTable table = new();
            table.Load(reader);

            var rows = new List<DataRow>(table.Rows.Cast<DataRow>());
            return new PagedQueryResult<DataRow>
            (
                rows,
                table.Rows.Count == pageSize,
                null
            );
        }
        finally
        {
            command.Connection.Close();
        }
    }

    public async Task<int?> GetTotalRecordCountAsync()
    {
        if (string.IsNullOrEmpty(totalRowCountCommandText)) return null;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = totalRowCountCommandText;
        cmd.Connection.Open();
        try
        {
            return await TryAsync(async () => (int?)await cmd.ExecuteScalarAsync());
        }
        finally
        {
            cmd.Connection.Close();
        }
    }


    private async Task<T> TryAsync<T>(Func<Task<T>> sqlRequest)
    {
        var retryCount = 0;
        Exception lastException;
        do
        {
            try
            {
                return await sqlRequest();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                lastException = ex;
            }
        } while (retryCount++ < MaxRetries);

        throw lastException;
    }

}

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Root16.Sprout.DataSources.Sql;

public class SqlReducingQuery(ILogger<SqlReducingQuery> logger, SqlConnection connection, string commandText, string? totalRowCountCommandText = null) : IPagedQuery<DataRow>
{
    private readonly ILogger<SqlReducingQuery> logger = logger;
    private readonly SqlConnection connection = connection;
    private readonly string commandText = commandText;
    private readonly string? totalRowCountCommandText = totalRowCountCommandText;

    const int MaxRetries = 10;

    public async Task<PagedQueryResult<DataRow>> GetNextPageAsync(int pageNumber, int pageSize, object? bookmark)
    {
        pageNumber = 1;
        using var command = connection.CreateCommand();
        command.CommandText = commandText.Trim();

        if (command.CommandText.EndsWith(";"))
        {
            command.CommandText = command.CommandText[..^1];
        }

        if (commandText.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
        {
            command.CommandText += $" OFFSET 0 ROWS FETCH NEXT {pageSize} ROWS ONLY";
        }
        else
        {
            command.CommandText += $" ORDER BY 1 OFFSET 0 ROWS FETCH NEXT {pageSize} ROWS ONLY";
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

    public async Task<int?> GetTotalRecordCountAsync(int batchSize, int? maxBatchCount)
    {
        if (string.IsNullOrWhiteSpace(totalRowCountCommandText)) return null;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = totalRowCountCommandText;
        cmd.Connection.Open();
        try
        {

            var totalCount = await TryAsync(async () => (int?)await cmd.ExecuteScalarAsync());

            return maxBatchCount is null
                ? totalCount
                : Math.Min((int) totalCount, batchSize * maxBatchCount.Value);
        }
        finally
        {
            cmd.Connection.Close();
        }
    }


    private async Task<T> TryAsync<T>(Func<Task<T>> sqlRequest)
    {
        var retryCount = 0;
        Exception? lastException = null;
        do
        {
            try
            {
                return await sqlRequest();
            }
            catch (Exception ex)
            {
                if (lastException is null || !ex.Message.Equals(lastException.Message, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogError(ex, ex.Message);
                }
                lastException = ex;
            }
        } while (retryCount++ < MaxRetries);

        throw lastException;
    }

}

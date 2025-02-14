using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using System.Data;

namespace Root16.Sprout.DataSources.Sql;

public class DynamicSqlPagedQuery(ILogger<DynamicSqlPagedQuery> logger, SqlConnection connection, Func<int, int, string> commandGenerator, string? totalRowCountCommandText = null) : IPagedQuery<DataRow>
{
    private readonly ILogger<DynamicSqlPagedQuery> logger = logger;
    private readonly SqlConnection connection = connection;
    private readonly Func<int, int, string> commandGenerator = commandGenerator;
    private readonly string? totalRowCountCommandText = totalRowCountCommandText;

    const int MaxRetries = 10;

    public async Task<PagedQueryResult<DataRow>> GetNextPageAsync(int pageNumber, int pageSize, object? bookmark)
    {
        using var command = connection.CreateCommand();
        command.CommandText = commandGenerator(pageNumber, pageSize);
        command.Connection.Open();

        var reader = await TryAsync(async () => await command.ExecuteReaderAsync(CommandBehavior.CloseConnection));
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

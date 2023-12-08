using Microsoft.Data.SqlClient;
using System.Data;

namespace Root16.Sprout.DataSources.Dataverse;

public class DynamicSqlPagedQuery : IPagedQuery<DataRow>
{
    private readonly SqlConnection connection;
    private readonly Func<int, int, string> commandGenerator;
    private readonly string? totalRowCountCommandText;

    public DynamicSqlPagedQuery(SqlConnection connection, Func<int, int, string> commandGenerator, string? totalRowCountCommandText = null)
    {
        this.connection = connection;
        this.commandGenerator = commandGenerator;
        this.totalRowCountCommandText = totalRowCountCommandText;
    }

    public async Task<PagedQueryResult<DataRow>> GetNextPageAsync(int pageNumber, int pageSize, object? bookmark)
    {
        using var command = connection.CreateCommand();
        command.CommandText = commandGenerator(pageNumber, pageSize);
        command.Connection.Open();
        var reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        try
        {
            DataTable table = new DataTable();
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
            return (int?)await cmd.ExecuteScalarAsync();
        }
        finally
        {
            cmd.Connection.Close();
        }
    }
}

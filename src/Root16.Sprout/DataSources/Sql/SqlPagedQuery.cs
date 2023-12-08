using Microsoft.Data.SqlClient;
using System.Data;

namespace Root16.Sprout.DataSources.Dataverse;

public class SqlPagedQuery : IPagedQuery<DataRow>
{
    private readonly SqlConnection connection;
    private readonly string commandText;
    private readonly string? totalRowCountCommandText;
    private readonly bool addPaging;

    public SqlPagedQuery(SqlConnection connection, string commandText, string? totalRowCountCommandText = null, bool addPaging = true)
    {
        this.connection = connection;
        this.commandText = commandText;
        this.totalRowCountCommandText = totalRowCountCommandText;
        this.addPaging = addPaging;
    }

    public async Task<PagedQueryResult<DataRow>> GetNextPageAsync(int pageNumber, int pageSize, object? bookmark)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = commandText;
            if (addPaging)
            {
                command.CommandText += $" OFFSET {pageNumber * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            }
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
    }

    public async Task<int?> GetTotalRecordCountAsync()
    {
        if (string.IsNullOrEmpty(totalRowCountCommandText)) return null;

        using (var cmd = connection.CreateCommand())
        {
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
}

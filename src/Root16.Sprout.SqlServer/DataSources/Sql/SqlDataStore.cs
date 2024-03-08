using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System.Data;
using Root16.Sprout.DataStores;

namespace Root16.Sprout.DataSources.Dataverse;

public class SqlDataStore
{
    private readonly string connectionString;
    private readonly ILogger<SqlDataStore> logger;
    private readonly SqlConnection connection;

    public SqlDataStore(string connectionString, ILogger<SqlDataStore> logger)
    {
        this.connectionString = connectionString;
        this.logger = logger;
        connection = new SqlConnection(connectionString);
    }

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
}


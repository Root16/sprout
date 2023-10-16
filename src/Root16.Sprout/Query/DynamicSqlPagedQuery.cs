using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq;
using System;

namespace Root16.Sprout.Query;

public class DynamicSqlPagedQuery : IPagedQuery<DataRow>
{
	private readonly SqlConnection connection;
	private readonly Func<int, int, string> commandGenerator;
	private readonly string totalRowCountCommandText;
	private int page;

	public DynamicSqlPagedQuery(SqlConnection connection, Func<int, int, string> commandGenerator, string totalRowCountCommandText = null)
	{
		this.connection = connection;
		this.commandGenerator = commandGenerator;
		this.totalRowCountCommandText = totalRowCountCommandText;
		MoreRecords = true;
	}

	public bool MoreRecords { get; private set; }

	public IReadOnlyList<DataRow> GetNextPage(int pageSize)
	{
		using var command = this.connection.CreateCommand();
		command.CommandText = commandGenerator(page, pageSize);
		command.Connection.Open();
		var reader = command.ExecuteReader(CommandBehavior.CloseConnection);
		try
		{
			DataTable table = new DataTable();
			table.Load(reader);
			page++;
			MoreRecords = table.Rows.Count == pageSize;

			var rows = new List<DataRow>(table.Rows.Cast<DataRow>());
			return rows;
		}
		finally
		{
			command.Connection.Close();
		}
	}

	public int? GetTotalRecordCount()
	{
		if (string.IsNullOrEmpty(totalRowCountCommandText)) return null;

		using var cmd = this.connection.CreateCommand();
		cmd.CommandText = totalRowCountCommandText;
		cmd.Connection.Open();
		try
		{
			return (int)cmd.ExecuteScalar();
		}
		finally
		{
			cmd.Connection.Close();
		}
	}
}

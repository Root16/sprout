using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Data;
using System.Linq;

namespace Root16.Sprout.Query;

public class SqlPagedQuery : IPagedQuery<DataRow>
{
	private readonly SqlConnection connection;
	private readonly string commandText;
	private readonly string totalRowCountCommandText;
	private readonly bool addPaging;
	private int page;

	public SqlPagedQuery(SqlConnection connection, string commandText, string totalRowCountCommandText = null, bool addPaging = true)
	{
		this.connection = connection;
		this.commandText = commandText;
		this.totalRowCountCommandText = totalRowCountCommandText;
		this.addPaging = addPaging;
		this.page = 0;
		MoreRecords = true;
	}

	public bool MoreRecords { get; private set; }

	public IReadOnlyList<DataRow> GetNextPage(int pageSize)
	{
		using (var command = this.connection.CreateCommand())
		{
			command.CommandText = commandText;
			if (addPaging)
			{
				command.CommandText += $" OFFSET {page * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";
			}
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
	}

	public int? GetTotalRecordCount()
	{
		if (string.IsNullOrEmpty(totalRowCountCommandText)) return null;

		using (var cmd = this.connection.CreateCommand())
		{
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
}

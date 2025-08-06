using Microsoft.Extensions.Logging;
using Root16.Sprout.Extensions;
using Root16.Sprout.Logging;
using System.Data;

namespace Root16.Sprout.SqlServer.DataSources.Sql;

public class DataRowBatchAnalyzer(
    ILogger<DataRowBatchAnalyzer> analyzer
    ) : BatchAnalyzer<DataRow>
{
    public override Audit GetDifference(string key, DataRow data, DataRow? previousData = null)
    {
        var changeRecord = new Audit(data.Table.TableName, string.Join(",", FormatValue(data.Table.PrimaryKey)), key, []);
        foreach (var column in data.Table.Columns.Cast<DataColumn>())
        {
            object previousValue = null;
            previousData?.TryGetValue(column.ColumnName, out previousValue);
            if (previousValue != data[column.ColumnName])
                changeRecord.Changes.Add(column.ColumnName, new ChangeValue($"{previousValue}", $"{data[column.ColumnName]}"));
        }

        return changeRecord;
    }

    public override string FormatValue(object value)
    {
        return value?.ToString() ?? string.Empty;
    }
}

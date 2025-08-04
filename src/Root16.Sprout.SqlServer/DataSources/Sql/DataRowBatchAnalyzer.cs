using Microsoft.Extensions.Logging;
using Root16.Sprout.Extensions;
using Root16.Sprout.Logging;
using System.Data;

namespace Root16.Sprout.SqlServer.DataSources.Sql;

public class DataRowBatchAnalyzer(
    ILogger<DataRowBatchAnalyzer> analyzer
    ) : BatchAnalyzer<DataRow>
{
    protected override ChangeRecord GetDifference(string key, DataRow data, DataRow? previousData = null)
    {
        var changeRecord = new ChangeRecord(data.Table.TableName, key, []);
        foreach (var column in data.Table.Columns.Cast<DataColumn>())
        {
            object previousValue = null;
            previousData?.TryGetValue(column.ColumnName, out previousValue);
            if(previousValue != data[column.ColumnName])
                changeRecord.Changes.Add(column.ColumnName, new ChangeValue($"{previousValue}", $"{data[column.ColumnName]}"));
        }

        return changeRecord;
    }
}

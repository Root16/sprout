using System.Data;
using System.Reflection;

namespace Root16.Sprout.Excel;

public abstract class ExcelClassMap<T> : IExcelMapper<T> where T : class, new()
{
	private readonly List<ExcelPropertyMap<T>> _maps = [];

	/// <summary>
	/// Bulk mapping from a dictionary of "PropertyName" => ["Excel column name"]
	/// Will match the first column that is not null if property has a meeting with multiple excel column names
	/// </summary>
	protected void MapFromDictionary(IDictionary<string, List<string>> columnMappings)
	{
		var missingMappings = columnMappings.Keys.Where(k => typeof(T).GetProperty(k, BindingFlags.Public | BindingFlags.Instance) is null);

		if (missingMappings.Any())
		{
			throw new ArgumentException($"Properties: '{string.Join(", ", missingMappings)} do not exist on {typeof(T).Name}");
		}

		var propertyToColumnNameMapping = columnMappings.SelectMany(kvp => kvp.Value.Select(value => new KeyValuePair<string, string>(kvp.Key, value)));

		foreach ((var propertyName, var excelColumnName) in propertyToColumnNameMapping)
		{
			var property = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

			var map = new ExcelPropertyMap<T>(property) { ColumnName = excelColumnName };
			_maps.Add(map);
		}
	}

	public List<T> Map(DataTable table)
	{
		var results = new List<T>();

		//For each row go through each property with a mapping and find the first mapping that matches with a value
		foreach (DataRow row in table.Rows)
		{
            var obj = new T();
            foreach (var property in _maps.Select(x => x.Property).ToHashSet())
			{
				foreach (var map in _maps.Where(x => x.Property.Equals(property)))
				{
                    if (!table.Columns.Contains(map.ColumnName))
                        continue;

                    var rawValue = row[map.ColumnName];
                    if (rawValue == DBNull.Value)
                        continue;

                    var converted = Convert.ChangeType(rawValue, map.Property.PropertyType);
                    map.Property.SetValue(obj, converted);
                    //We match the first value that is not null
                    break;
                }
			}
            results.Add(obj);
        }

		return results;
	}

	protected class ExcelPropertyMap<TT>
	{
		public PropertyInfo Property { get; }
		public string ColumnName { get; set; }

        public ExcelPropertyMap(PropertyInfo property)
		{
			Property = property;
			ColumnName = property.Name; // default fallback
		}
	}

	protected class ExcelPropertyMapBuilder
	{
		private readonly ExcelPropertyMap<T> _map;
		public ExcelPropertyMapBuilder(ExcelPropertyMap<T> map) => _map = map;
		public ExcelPropertyMapBuilder Name(string columnName)
		{
			_map.ColumnName = columnName;
			return this;
		}
	}
}

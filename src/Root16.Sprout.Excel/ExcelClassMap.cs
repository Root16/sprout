using System.Data;
using System.Reflection;
using System.Linq.Expressions;

namespace Root16.Sprout.Excel;

public abstract class ExcelClassMap<T> : IExcelMapper<T> where T : class, new()
{
	private readonly List<ExcelPropertyMap<T>> _maps = new();

	/// <summary>
	/// Bulk mapping from a dictionary of "Excel column name" => "PropertyName"
	/// </summary>
	protected void MapFromDictionary(IDictionary<string, string> columnMappings)
	{
		foreach (var kvp in columnMappings)
		{
			var property = typeof(T).GetProperty(kvp.Value, BindingFlags.Public | BindingFlags.Instance);
			if (property == null)
				throw new ArgumentException($"Property '{kvp.Value}' does not exist on {typeof(T).Name}");

			var map = new ExcelPropertyMap<T>(property) { ColumnName = kvp.Key };
			_maps.Add(map);
		}
	}

	public List<T> Map(DataTable table)
	{
		var results = new List<T>();

		foreach (DataRow row in table.Rows)
		{
			var obj = new T();
			foreach (var map in _maps)
			{
				if (!table.Columns.Contains(map.ColumnName))
					continue;

				var rawValue = row[map.ColumnName];
				if (rawValue == DBNull.Value)
					continue;

				var converted = Convert.ChangeType(rawValue, map.Property.PropertyType);
				map.Property.SetValue(obj, converted);
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

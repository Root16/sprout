using System.Data;

namespace Root16.Sprout.Excel;

public interface IExcelMapper<T>
{
	List<T> Map(DataTable table);
}

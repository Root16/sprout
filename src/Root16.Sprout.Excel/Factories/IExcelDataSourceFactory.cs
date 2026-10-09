namespace Root16.Sprout.Excel.Factories;

public interface IExcelDataSourceFactory
{
    ExcelDataSource<T> GetExcelDataSourceByName<T>(string recordName) where T : class;
}

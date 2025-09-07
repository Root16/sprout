using ExcelDataReader;
using Microsoft.Extensions.DependencyInjection;

namespace Root16.Sprout.Excel.Extensions;

public static partial class ServiceCollectionExtensions
{
	public static IServiceCollection RegisterExcelDataSource<T, TMapper>(
		this IServiceCollection services,
		string excelDataSourceName,
		string path,
		int tabIndex = 0
		)
		where T : class, new()
		where TMapper : IExcelMapper<T>, new()
	{
		System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

		using var stream = File.Open(path, FileMode.Open, FileAccess.Read);
		using var reader = ExcelReaderFactory.CreateReader(stream);

		var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
		{
			ConfigureDataTable = _ => new ExcelDataTableConfiguration
			{
				UseHeaderRow = true
			}
		});

		var table = dataSet.Tables[tabIndex];
		var mapper = new TMapper();
		var records = mapper.Map(table);

		services.AddKeyedSingleton(excelDataSourceName, new ExcelDataSource<T>(records));
		return services;
	}
}

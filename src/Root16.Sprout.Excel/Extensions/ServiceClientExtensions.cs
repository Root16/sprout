using System;
using System.Data;
using System.IO;
using ExcelDataReader;
using Microsoft.Extensions.DependencyInjection;

namespace Root16.Sprout.Excel.Extensions;

public static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers an Excel data source with the specified name, path, and tab index.
    /// The data will be mapped to the specified type T using the provided mapper TMapper.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TMapper"></typeparam>
    /// <param name="services"></param>
    /// <param name="excelDataSourceName"></param>
    /// <param name="path"></param>
    /// <param name="tabIndex"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static IServiceCollection RegisterExcelDataSource<T, TMapper>(
        this IServiceCollection services,
        string excelDataSourceName,
        string path,
        int tabIndex = 0)
        where T : class, new()
        where TMapper : IExcelMapper<T>, new()
    {
        ValidateCommonInputs(services, excelDataSourceName, path);

        if (tabIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tabIndex), "Tab index cannot be negative.");
        }

        var table = GetDataTable(path, dataSet =>
        {
            if (tabIndex >= dataSet.Tables.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(tabIndex), $"The Excel file contains {dataSet.Tables.Count} worksheet(s), but tab index {tabIndex} was requested.");
            }
            return dataSet.Tables[tabIndex];
        });

        return RegisterData<T, TMapper>(services, excelDataSourceName, table);
    }

    /// <summary>
    /// Registers an Excel data source with the specified name, path, and worksheet name.
    /// The data will be mapped to the specified type T using the provided mapper TMapper.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TMapper"></typeparam>
    /// <param name="services"></param>
    /// <param name="excelDataSourceName"></param>
    /// <param name="path"></param>
    /// <param name="worksheetName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static IServiceCollection RegisterExcelDataSource<T, TMapper>(
        this IServiceCollection services,
        string excelDataSourceName,
        string path,
        string worksheetName)
        where T : class, new()
        where TMapper : IExcelMapper<T>, new()
    {
        ValidateCommonInputs(services, excelDataSourceName, path);

        if(string.IsNullOrWhiteSpace(worksheetName))
        {
            throw new ArgumentException("Worksheet name cannot be null or whitespace.", nameof(worksheetName));
        }

        var table = GetDataTable(path, dataSet =>
            dataSet.Tables[worksheetName] ??
            throw new ArgumentException($"Worksheet '{worksheetName}' was not found in the Excel file.", nameof(worksheetName))
        );

        return RegisterData<T, TMapper>(services, excelDataSourceName, table);
    }

    private static void ValidateCommonInputs(IServiceCollection services, string name, string path)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("DataSource name cannot be null or whitespace.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"The specified Excel file was not found at path: {path}", path);
        }
    }

    private static DataTable GetDataTable(string path, Func<DataSet, DataTable> tableSelector)
    {
        try
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = true
                }
            });

            return tableSelector(dataSet);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException($"Unable to access the Excel file at '{path}'. Ensure the file is not locked by another process.", ex);
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not ArgumentOutOfRangeException)
        {
            throw new InvalidOperationException($"An error occurred while parsing the Excel file at '{path}'.", ex);
        }
    }

    private static IServiceCollection RegisterData<T, TMapper>(IServiceCollection services, string name, DataTable table) where T : class, new() where TMapper : IExcelMapper<T>, new()
    {
        var mapper = new TMapper();
        var records = mapper.Map(table);

        services.AddKeyedSingleton(name, new ExcelDataSource<T>(records));
        return services;
    }
}
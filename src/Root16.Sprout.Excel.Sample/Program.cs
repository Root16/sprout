using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Root16.Sprout;
using Root16.Sprout.Excel.Extensions;
using Root16.Sprout.Excel.Factories;
using Root16.Sprout.Excel.Sample.Models;
using Root16.Sprout.Excel.Sample.Steps;
using Root16.Sprout.DataSources.Dataverse;


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddSprout();
builder.Services.AddSproutDataverse();
builder.Services.AddSproutExcel();
builder.Services.AddDataverseDataSource("Dataverse");

//builder.Services.RegisterExcelDataSource<TestClass1, TestClass1Map>("EXCEL1", @"..\..\..\Data\test.xlsx");
builder.Services.RegisterExcelDataSource<TestClass1, TestClass1Map>("EXCEL1", @"..\..\..\Data\test.xlsx", tabIndex: 1);

builder.Services.RegisterStep<ExampleExcelStep1>();

builder.Logging.AddFilter("Microsoft.PowerPlatform.Dataverse", LogLevel.Warning);

var host = builder.Build();
host.Start();

var factory = host.Services.GetRequiredService<IExcelDataSourceFactory>();

var runtime = host.Services.GetRequiredService<IIntegrationRuntime>();

await runtime.RunAllStepsAsync();

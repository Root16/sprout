using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Root16.Sprout;
using Root16.Sprout.CSV;
using Root16.Sprout.CSV.Factories;
using Root16.Sprout.CSV.Sample.Models;
using Root16.Sprout.CSV.Sample.Steps;
using Root16.Sprout.DependencyInjection;
using System.Diagnostics;
using System.Runtime.InteropServices;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddSprout();
builder.Services.AddSproutCSV();

builder.Services.RegisterCSVDataSource<TestClass1, TestClass1Map>("CSV1", @"..\..\..\Data\test1.csv");
builder.Services.RegisterCSVDataSource<TestClass1, TestClass1Map>("CSV1Copy", @"..\..\..\Data\test1copy.csv");

builder.Services.RegisterStep<ExampleCSVStep1>();
builder.Services.RegisterStep<ExampleCSVStep2>();
builder.Services.RegisterStep<ExampleCSVStep3>();

//Hide Logs Below Warning for Dataverse connections
builder.Logging.AddFilter("Microsoft.PowerPlatform.Dataverse", LogLevel.Warning);

var host = builder.Build();
host.Start();

var factory = host.Services.GetRequiredService<ICSVDataSourceFactory>();

var runtime = host.Services.GetRequiredService<IIntegrationRuntime>();

await runtime.RunAllStepsAsync();
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Root16.Sprout;
using Root16.Sprout.BatchProcessing;
using Root16.Sprout.DataSources.Sql;
using Root16.Sprout.Sample.SqlServer;
using System;
using System.Diagnostics;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddSprout();
builder.Services.AddSproutDataverse();

builder.Services.RegisterStep<SampleSQLStep>();
builder.Services.RegisterStep<SampleSQLFileStep>();
builder.Services.RegisterStep<SampleSQLFilesStep>();

builder.Services.RegisterStep<GenericSampleSQLStep>("SecondSQLDataSourceStep", (serviceProvider, myKey) =>
{
    return new GenericSampleSQLStep(myKey as string, serviceProvider, serviceProvider.GetRequiredService<BatchProcessor>());
});
builder.Services.RegisterStep<GenericSampleSQLStep>("ThirdSQLDataSourceStep", (serviceProvider, myKey) =>
{
    return new GenericSampleSQLStep(myKey as string, serviceProvider, serviceProvider.GetRequiredService<BatchProcessor>());
});

//Hide Logs Below Warning for Dataverse connections
builder.Logging.AddFilter("Microsoft.PowerPlatform.Dataverse", LogLevel.Warning);

builder.Services.AddKeyedScoped<SqlDataSource>(null, (serviceProvider, key) =>
{
    var connectionString = "Server=localhost\\MSSQLSERVER01;Database=master;Integrated Security=True;Trusted_Connection=True;TrustServerCertificate=true;";
    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
    var dataSource = new SqlDataSource(connectionString, loggerFactory);

    return dataSource;
});

builder.Services.AddKeyedScoped<SqlDataSource>("SecondSQLDataSourceStep", (serviceProvider, key) =>
{
    var connectionString = "Server=localhost\\MSSQLSERVER01;Database=master;Integrated Security=True;Trusted_Connection=True;TrustServerCertificate=true;";
    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
    var dataSource = new SqlDataSource(connectionString, loggerFactory);

    return dataSource;
});

builder.Services.AddKeyedScoped<SqlDataSource>("ThirdSQLDataSourceStep", (serviceProvider, key) =>
{
    var connectionString = "Server=localhost\\MSSQLSERVER01;Database=master;Integrated Security=True;Trusted_Connection=True;TrustServerCertificate=true;";
    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
    var dataSource = new SqlDataSource(connectionString, loggerFactory);

    return dataSource;
});

var host = builder.Build();
host.Start();

var runtime = host.Services.GetRequiredService<IIntegrationRuntime>();


await runtime.RunAllStepsAsync();

Console.WriteLine("Sprout Sample Complete.");
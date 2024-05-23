using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Root16.Sprout;
using Root16.Sprout.DataSources.Sql;
using Root16.Sprout.Sample.SqlServer;
using System.Diagnostics;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddSprout();
builder.Services.AddSproutDataverse();

builder.Services.RegisterStep<SampleSQLStep>();

//Hide Logs Below Warning for Dataverse connections
builder.Logging.AddFilter("Microsoft.PowerPlatform.Dataverse", LogLevel.Warning);

builder.Services.AddScoped<SqlDataSource>((serviceProvider) =>
{
    var connectionString = "Server=localhost\\MSSQLSERVER01;Database=master;Integrated Security=True;Trusted_Connection=True;TrustServerCertificate=true;";
    Debugger.Break();
    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
    var dataSource = new SqlDataSource(connectionString, loggerFactory);

    return dataSource;
});

var host = builder.Build();
host.Start();

var runtime = host.Services.GetRequiredService<IIntegrationRuntime>();


await runtime.RunAllStepsAsync();

Console.WriteLine("Sprout Sample Complete.");
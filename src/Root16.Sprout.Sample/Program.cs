using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Root16.Sprout;
using Root16.Sprout.Data;
using Root16.Sprout.Extensions;
using Root16.Sprout.Processors;
using Root16.Sprout.Progress;
using Root16.Sprout.Sample;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

// TODO: move these to common extension methods
builder.Services.AddSingleton<IIntegrationRuntime, IntegrationRuntime>();
builder.Services.AddTransient<BatchProcessBuilder>();
builder.Services.AddTransient(typeof(BatchProcessor<,>));
builder.Services.AddSingleton<IProgressListener, ConsoleProgressListener>();
builder.Services.AddTransient<DataverseDataSource>();

builder.Services.AddStep<TestStep>();
builder.Services.AddTransient(services =>
{
    var config = services.GetRequiredService<IConfiguration>();
    return new ServiceClient(config.GetConnectionString("Dataverse"));
});
var host = builder.Build();
host.Start();

var runtime = host.Services.GetRequiredService<IIntegrationRuntime>();
await runtime.RunStepAsync<TestStep>();
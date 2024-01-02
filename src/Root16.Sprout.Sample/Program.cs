using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Root16.Sprout;
using Root16.Sprout.DataSources;
using Root16.Sprout.DataSources.Dataverse;
using Root16.Sprout.DependencyInjection;
using Root16.Sprout.Sample;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddSprout();

builder.Services.AddStep<TestStep>();
builder.Services.AddDataverseDataSource("Dataverse");

builder.Services.AddSingleton(
    _ => new MemoryDataSource<Contact>(SampleData.GenerateSampleData(8000))
);

var host = builder.Build();
host.Start();

var runtime = host.Services.GetRequiredService<IIntegrationRuntime>();
var finishedStepName = await runtime.RunStepAsync<TestStep>();

Console.WriteLine($"{finishedStepName} has completed!");

Console.WriteLine("Sprout Sample Complete.");
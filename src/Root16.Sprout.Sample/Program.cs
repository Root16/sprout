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
using Root16.Sprout.Step;
using System;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

// TODO: move these to common extension methods
builder.Services.AddSingleton<IIntegrationRuntime, IntegrationRuntime>();
builder.Services.AddTransient<BatchRunner>();
builder.Services.AddSingleton<IProgressListener, ConsoleProgressListener>();
builder.Services.AddTransient<DataverseDataSource>();
builder.Services.AddTransient<EntityReducer>();

builder.Services.AddStep<TestStep>();
builder.Services.AddTransient(services =>
{
    var config = services.GetRequiredService<IConfiguration>();
    return new ServiceClient(config.GetConnectionString("Dataverse"));
});

builder.Services.AddSingleton(services => 
    new MemoryDataSource<Contact>(new[]
    {
        new Contact { FirstName = "Corey", LastName = "Test" },
        new Contact { FirstName = "Corey", LastName = "Test2" },
        new Contact { FirstName = "Corey", LastName = "Test3" },
    })
);

var host = builder.Build();
host.Start();

var runtime = host.Services.GetRequiredService<IIntegrationRuntime>();
await runtime.RunStepAsync<TestStep>();

Console.WriteLine("Complete.");
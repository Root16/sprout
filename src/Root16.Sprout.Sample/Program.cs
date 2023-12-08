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
    _ => new MemoryDataSource<Contact>(new[]
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
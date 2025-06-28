using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Root16.Sprout;
using Root16.Sprout.DataSources;
using Root16.Sprout.DataSources.Dataverse;
using Root16.Sprout.Sample;
using Root16.Sprout.Sample.CreatesAndUpdates;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddSprout();
builder.Services.AddSproutDataverse();

builder.Services.RegisterStep<CreateContactTestStep>();
builder.Services.RegisterStep<UpdateContactTestStep>();
builder.Services.RegisterStep<DeleteTestStep>();
builder.Services.RegisterStep<ReportErrorsStep>();
builder.Services.AddDataverseDataSource("Dataverse");

builder.Services.AddSingleton(
    _ => new MemoryDataSource<CreateContact>(SampleData.GenerateCreateContactSampleData(amount: 2000))
);

builder.Services.AddSingleton(
    _ => new MemoryDataSource<UpdateContact>(SampleData.GenerateUpdateContactSampleData(amount: 250, startNumber: 1975))
);


var host = builder.Build();
host.Start();

var runtime = host.Services.GetRequiredService<IIntegrationRuntime>();

await runtime.RunStepAsync<DeleteTestStep>();
await runtime.RunStepAsync<CreateContactTestStep>();

//Only the overlapping amount should be updated in this step as the data operation is "Update".
//In the case above it's set so that only 25 of the possible 250 entities are update
await runtime.RunStepAsync<UpdateContactTestStep>();

//Log any errors along with the `Target` Entity that created each error
await runtime.RunStepAsync<ReportErrorsStep>();

Console.WriteLine("Sprout Sample Complete.");
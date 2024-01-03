using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Root16.Sprout;
using Root16.Sprout.DataSources;
using Root16.Sprout.DataSources.Dataverse;
using Root16.Sprout.DependencyInjection;
using Root16.Sprout.Sample;
using Root16.Sprout.Sample.Models;


// TODO: There's gotta be a better way to make a list of dependent items
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddSprout();

//Test - RunAllStepsAtTheSameTime
builder.Services.RegisterStep<ContactTestStep>();
builder.Services.RegisterStep<AccountTestStep>();

//Test - RunAllStepsWithDependenciesOneAtATime
builder.Services.RegisterStep<ContactTestStep>();
builder.Services.RegisterStep<AccountTestStep>(new List<string> { typeof(ContactTestStep).Name });

//Test - RunAllStepsWithDependenciesAtTheSameTime
builder.Services.RegisterStep<ContactTestStep>();
builder.Services.RegisterStep<AccountTestStep>(new List<string> { typeof(ContactTestStep).Name });
//TODO: Add 2 independent steps (making 2 other entities), and a step that's dependent on Contact and Account (Create Opportunity?)

//Test - RunAllStepsWithDependenciesSetAmountAtATime
builder.Services.RegisterStep<ContactTestStep>();
builder.Services.RegisterStep<AccountTestStep>(new List<string> { typeof(ContactTestStep).Name });
//TODO: Add 2 independent steps (making 2 other entities), and a step that's dependent on Contact and Account (Create Opportunity?)


builder.Services.AddDataverseDataSource("Dataverse");

//Hide Logs Below Warning for Dataverse connections
builder.Logging.AddFilter("Microsoft.PowerPlatform.Dataverse", LogLevel.Warning);

builder.Services.AddSingleton(
    _ => new MemoryDataSource<Contact>(SampleData.GenerateSampleContacts(500))
    );
builder.Services.AddSingleton(
    _ => new MemoryDataSource<Account>(SampleData.GenerateSampleAccounts(500))
    );

var host = builder.Build();
host.Start();

var runtime = host.Services.GetRequiredService<IIntegrationRuntime>();


//Test - RunAllStepsAtTheSameTime
//Steps Will Run And Log At The Same Time
await foreach (var finishedStep in runtime.RunAllStepsAtTheSameTime())
{
    Console.WriteLine($"Step Finished - {finishedStep}");
}

//Test - RunAllStepsWithDependenciesOneAtATime
//Contact Step will run and then account step will run
await foreach (var finishedStep in runtime.RunAllStepsWithDependenciesOneAtATime())
{
    Console.WriteLine($"Step Finished - {finishedStep}");
}

//Test - RunAllStepsWithDependenciesAtTheSameTime
// Contact, and both new steps will run at the same time and whenever contact is and one of the other steps isdone then
// account will run, and when account is done the step that dependent on both contact and account will run
await foreach (var finishedStep in runtime.RunAllStepsWithDependenciesAtTheSameTime())
{
    Console.WriteLine($"Step Finished - {finishedStep}");
}

//Test - RunAllStepsWithDependenciesSetAmountAtATime
// Contact and one of the other steps will run at the same time and whenever contact is done then another step will run and when
// the 2nd step is done then account will run and when account is done the step that is dependent on both contact and account will run
await foreach (var finishedStep in runtime.RunAllStepsWithDependenciesSetAmountAtATime(2))
{
    Console.WriteLine($"Step Finished - {finishedStep}");
}

Console.WriteLine("Sprout Sample Complete.");
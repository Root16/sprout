using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Root16.Sprout;
using Root16.Sprout.DataSources;
using Root16.Sprout.DataSources.Dataverse;
using Root16.Sprout.Sample;
using Root16.Sprout.Sample.ParallelSteps;
using Root16.Sprout.Sample.ParallelSteps.Models;
using Root16.Sprout.Sample.ParallelSteps.TestSteps;
using Root16.Sprout.Sample.TestSteps;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddSprout();
builder.Services.AddSproutDataverse();
builder.Services.AddSpectreConsole();

builder.Services.RegisterStep<ContactTestStep>();
builder.Services.RegisterStep<TaskTestStep>();
builder.Services.RegisterStep<LetterTestStep>(nameof(TaskTestStep));
builder.Services.RegisterStep<AccountTestStep>(nameof(ContactTestStep));
builder.Services.RegisterStep<EmailTestStep>();

//builder.Services.RegisterStep<AccountInvalidDependencyTestStep>(nameof(ContactInvalidDependencyTestStep));
//builder.Services.RegisterStep<ContactInvalidDependencyTestStep>(nameof(AccountInvalidDependencyTestStep));

builder.Services.AddDataverseDataSource("Dataverse");

//Hide Logs Below Warning for Dataverse connections
builder.Logging.AddFilter("Microsoft.PowerPlatform.Dataverse", LogLevel.Warning);

builder.Services.AddSingleton(
    _ => new MemoryDataSource<Contact>(SampleData.GenerateSampleContacts(2000))
    );
builder.Services.AddSingleton(
    _ => new MemoryDataSource<Account>(SampleData.GenerateSampleAccounts(2000))
    );
builder.Services.AddSingleton(
    _ => new MemoryDataSource<TaskData>(SampleData.GenerateSampleTasks(2000))
    );
builder.Services.AddSingleton(
    _ => new MemoryDataSource<Letter>(SampleData.GenerateSampleLetters(2000))
    );
builder.Services.AddSingleton(
    _ => new MemoryDataSource<Email>(SampleData.GenerateSampleEmails(2000))
    );
var host = builder.Build();
host.Start();

var runtime = host.Services.GetRequiredService<IIntegrationRuntime>();

//This will throw an error unless AccountInvalidDependencyTestStep, and ContactInvalidDependencyTestStep are both commented out
// Contact and Task will run, then Email and Account and then letter. Takes into account dependencies, but then also still only runs 2 at a time
await runtime.RunAllStepsAsync(2, finishedStep =>
{
    Console.WriteLine($"Step Finished - {finishedStep}");
});

Console.WriteLine("Sprout Sample Complete.");
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Root16.Sprout;
using Root16.Sprout.DataSources;
using Root16.Sprout.DataSources.Dataverse;
using Root16.Sprout.DependencyInjection;
using Root16.Sprout.Sample;
using Root16.Sprout.Sample.ParallelSteps;
using Root16.Sprout.Sample.ParallelSteps.Models;
using Root16.Sprout.Sample.ParallelSteps.TestSteps;
using Root16.Sprout.Sample.TestSteps;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddSprout();

builder.Services.RegisterStep<ContactTestStep>();
builder.Services.RegisterStep<TaskTestStep>();
builder.Services.RegisterStep<LetterTestStep>(nameof(TaskTestStep));
builder.Services.RegisterStep<AccountTestStep>(nameof(ContactTestStep));
builder.Services.RegisterStep<EmailTestStep>();


builder.Services.AddDataverseDataSource("Dataverse");

//Hide Logs Below Warning for Dataverse connections
builder.Logging.AddFilter("Microsoft.PowerPlatform.Dataverse", LogLevel.Warning);
builder.Services.AddSingleton(
    _ => new MemoryDataSource<Contact>(SampleData.GenerateSampleContacts(200))
    );
builder.Services.AddSingleton(
    _ => new MemoryDataSource<Account>(SampleData.GenerateSampleAccounts(200))
    );
builder.Services.AddSingleton(
    _ => new MemoryDataSource<TaskData>(SampleData.GenerateSampleTasks(200))
    );
builder.Services.AddSingleton(
    _ => new MemoryDataSource<Letter>(SampleData.GenerateSampleLetters(200))
    );
builder.Services.AddSingleton(
    _ => new MemoryDataSource<Email>(SampleData.GenerateSampleEmails(200))
    );
var host = builder.Build();
host.Start();

var runtime = host.Services.GetRequiredService<IIntegrationRuntime>();

// Contact and Task will run, then Email and Account and then letter. Takes into account dependencies, but then also still only runs 2 at a time
await runtime.RunAllStepsAsync(2, finishedStep =>
{
    Console.WriteLine($"Step Finished - {finishedStep}");
});

Console.WriteLine("Sprout Sample Complete.");
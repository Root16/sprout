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
using Root16.Sprout.Sample.TestSteps;


// TODO: There's gotta be a better way to make a list of dependent items
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddSprout();

builder.Services.RegisterStep<ContactTestStep>();
builder.Services.RegisterStep<TaskTestStep>();
builder.Services.RegisterStep<LetterTestStep>(new List<string> { typeof(TaskTestStep).Name });
builder.Services.RegisterStep<AccountTestStep>(new List<string> { typeof(ContactTestStep).Name });
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


////Test - RunAllStepsAtTheSameTime
////All Of The Steps Will Run At The Same Time. Dependency Is Not Taken Into Account
//await foreach (var finishedStep in runtime.RunAllStepsAtTheSameTime())
//{
//    Console.WriteLine($"Step Finished - {finishedStep}");
//}

////Test - RunAllStepsWithDependenciesOneAtATime
////Contact Will Run, Then Task, Then Email, Then Account, Then Letter
//await foreach (var finishedStep in runtime.RunAllStepsWithDependenciesOneAtATime())
//{
//    Console.WriteLine($"Step Finished - {finishedStep}");
//}

////Test - RunAllStepsWithDependenciesAtTheSameTime
//// Contact, Task, and Email will run at the same time, then account, and then contact. Only waits for dependencies to hit
//await foreach (var finishedStep in runtime.RunAllStepsWithDependenciesAtTheSameTime())
//{
//    Console.WriteLine($"Step Finished - {finishedStep}");
//}

//Test - RunAllStepsWithDependenciesSetAmountAtATime
// Contact and Task will run, then Email and Account and then letter. Takes into account dependencies, but then also still only runs 2 at a time
await foreach (var finishedStep in runtime.RunAllStepsWithDependenciesSetAmountAtATime(2))
{
    Console.WriteLine($"Step Finished - {finishedStep}");
}

Console.WriteLine("Sprout Sample Complete.");
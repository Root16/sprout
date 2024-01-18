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

builder.Services.RegisterStep<CreateContactTestStep>();
builder.Services.RegisterStep<TaskTestStep>();
builder.Services.RegisterStep<LetterTestStep>(nameof(TaskTestStep));
builder.Services.RegisterStep<AccountTestStep>(nameof(CreateContactTestStep));
builder.Services.RegisterStep<EmailTestStep>();
builder.Services.RegisterStep<UpdateContactTestStep>(nameof(UpdateContactTestStep));

builder.Services.AddDataverseDataSource("Dataverse");

//Hide Logs Below Warning for Dataverse connections
builder.Logging.AddFilter("Microsoft.PowerPlatform.Dataverse", LogLevel.Warning);
builder.Services.AddSingleton(
    _ => new MemoryDataSource<Account>(SampleData.GenerateSampleAccounts(amount: 200))
    );
builder.Services.AddSingleton(
    _ => new MemoryDataSource<TaskData>(SampleData.GenerateSampleTasks(amount: 200))
    );
builder.Services.AddSingleton(
    _ => new MemoryDataSource<Letter>(SampleData.GenerateSampleLetters(amount: 200))
    );
builder.Services.AddSingleton(
    _ => new MemoryDataSource<Email>(SampleData.GenerateSampleEmails(amount: 200))
    );
builder.Services.AddSingleton(
    _ => new MemoryDataSource<CreateContact>(SampleData.GenerateCreateContactSampleData(amount: 250))
);

builder.Services.AddSingleton(
    _ => new MemoryDataSource<UpdateContact>(SampleData.GenerateUpdateContactSampleData(amount: 250, startNumber: 225))
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
////CreateContact Will Run, Then Task, Then Email, Then Account, Then UpdateContact, and Then Letter
//await foreach (var finishedStep in runtime.RunAllStepsWithDependenciesOneAtATime())
//{
//    Console.WriteLine($"Step Finished - {finishedStep}");
//}

//Test - RunAllStepsWithDependenciesAtTheSameTime
// CreateContact, Task, and Email will run at the same time, then account, and then UpdateContact, and then Letter.
await foreach (var finishedStep in runtime.RunAllStepsWithDependenciesAtTheSameTime())
{
    Console.WriteLine($"Step Finished - {finishedStep}");
}

Console.WriteLine("Sprout Sample Complete.");
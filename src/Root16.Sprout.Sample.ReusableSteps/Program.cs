using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Root16.Sprout;
using Root16.Sprout.BatchProcessing;
using Root16.Sprout.DataSources;
using Root16.Sprout.DataSources.Dataverse;
using Root16.Sprout.Dataverse.Extensions;
using Root16.Sprout.Sample.ReusableSteps.Steps;
using Root16.Sprout.Sample.StepRegistration.Models;
using Root16.Sprout.Sample.StepRegistration.Steps;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddSprout();
builder.Services.AddSproutDataverse();
builder.Services.AddDataverseDataSource("dataverse");

var value1 = "Value1";
var value2 = "Value2";

// Register the step with no alternate name and only getting dependency injection from the DI container
builder.Services.AddSingleton<MemoryDataSource<SampleClass>>();

//Register the step with no alternate name and only getting dependency injection from the DI container
builder.Services.RegisterStep<StandardStep>();

// Register the step with no alternate name and passing the parameters directly to the constructor
// Making prerequeisite step a single parameter
builder.Services.RegisterStep<ReusableStepWithSingleParam>((sp, _) =>
{
    var memoryDS = sp.GetRequiredService<MemoryDataSource<SampleClass>>();
    var dataverseDataSource = sp.GetRequiredService<DataverseDataSource>();
    var reducer = sp.GetRequiredService<EntityOperationReducer>();
    var batchProcessor = sp.GetRequiredService<BatchProcessor>();
    return new ReusableStepWithSingleParam(memoryDS, dataverseDataSource, (value1, value2), reducer, batchProcessor);
}, nameof(StandardStep));

// Register the step with an alternate name and passing the parameters directly to the constructor
// Making prerequeisite step a regular params
builder.Services.RegisterStep<ReusableStepWithSingleParam>("ReusableStepName1", (sp, _) =>
{
    var memoryDS = sp.GetRequiredService<MemoryDataSource<SampleClass>>();
    var dataverseDataSource = sp.GetRequiredService<DataverseDataSource>();
    var reducer = sp.GetRequiredService<EntityOperationReducer>();
    var batchProcessor = sp.GetRequiredService<BatchProcessor>();
    return new ReusableStepWithSingleParam(memoryDS, dataverseDataSource, (value1, value2), reducer, batchProcessor);
}, nameof(StandardStep), nameof(ReusableStepWithSingleParam));

// Register the step with an alternate name and passing the parameter as part of registration.
// Making prequresities a List/Array
string[] preRegStepsFor2 = [nameof(StandardStep), nameof(ReusableStepWithSingleParam), "ReusableStepName1"];
builder.Services.RegisterStepWithArgument<ReusableStepWithSingleParam, (string, string)>("ReusableStepName2", (value1, value2),
    preRegStepsFor2);

// Register the step with an alternate name and passing the tuple parameter as part of registration.
// Making prequresities a List inline
builder.Services.RegisterStepWithArguments<ReusableStepWithSingleParam>("ReusableStepName3", [(value1, value2)],
    [nameof(StandardStep), nameof(ReusableStepWithSingleParam), "ReusableStepName1", "ReusableStepName2"]);

// Register the step with an alternate name and passing the parameters directly to the constructor
// Making prequresities an IEnumerable to simulate a more complex scenario where the preregistration steps are generated dynamically
IEnumerable<string> preRegStepsFor4 = [nameof(StandardStep), nameof(ReusableStepWithSingleParam), "ReusableStepName1", "ReusableStepName2", "ReusableStepName3"];
builder.Services.RegisterStep<ReusableStepWithMultipleParams>("ReusableStepName4", (sp, _) =>
{
    return new ReusableStepWithMultipleParams(
        sp.GetRequiredService<MemoryDataSource<SampleClass>>(),
        value1,
        sp.GetRequiredService<DataverseDataSource>(),
        sp.GetRequiredService<EntityOperationReducer>(),
        value2,
        sp.GetRequiredService<BatchProcessor>());
}, preRegStepsFor4);

// Register the step with an alternate name and passing the parameters to the registration method.
// Registration method will handle the creation of the step and its dependencies.
// Registration method does not care if parameters are at the beginning, middle, or end or even next to each other.
// It will handle the creation of the step and its dependencies and pass the parameters to the constructor in the correct order that they are passed in
// Making prequresities generated during the registration process
List<string> preReqStepsFor5 = [nameof(StandardStep), nameof(ReusableStepWithSingleParam), "ReusableStepName1", "ReusableStepName2", "ReusableStepName3", "ReusableStepName4", "RandomStringThatWouldBreakThis"];
builder.Services.RegisterStepWithArguments<ReusableStepWithMultipleParams>("ReusableStepName5", [value1, value2],
    preReqStepsFor5.Where(x => x.Contains("step", StringComparison.OrdinalIgnoreCase)));

var host = builder.Build();
host.Start();

var runtime = host.Services.GetRequiredService<IIntegrationRuntime>();

await runtime.RunAllStepsAsync();
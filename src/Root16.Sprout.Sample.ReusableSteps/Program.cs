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

builder.Services.AddSingleton<MemoryDataSource<SampleClass>>();

// We will build up this list as we register steps to pass into the subsequent steps
// to simulate a more complex scenario where the preregistration steps are generated dynamically.
List<string> runningPreReqs = [];

#region RegisterStep

// Register the step with no alternate name and only getting dependency injection from the DI container.
// Making prerequisite step empty.
// OVERLOAD HIT: params string[]
builder.Services.RegisterStep<StandardStep>();
runningPreReqs.Add(nameof(StandardStep));

// Register the step with no alternate name and only getting dependency injection from the DI container.
// Making prerequisites an IEnumerable to simulate a more complex scenario where the preregistration steps are generated dynamically.
// OVERLOAD HIT: IEnumerable<string>
IEnumerable<string> prereqs1 = [.. runningPreReqs];
builder.Services.RegisterStep<StandardStep>(prereqs1);

#endregion

#region RegisterStepWithName

// Register the step with an alternate name and only getting dependency injection from the DI container.
// Making prerequisite step a regular params (comma-separated strings).
// OVERLOAD HIT: params string[]
builder.Services.RegisterStep<StandardStep>("NamedStandard1", nameof(StandardStep));
runningPreReqs.Add("NamedStandard1");

// Register the step with an alternate name and only getting dependency injection from the DI container.
// Making prerequisites a HashSet to demonstrate collection flexibility without needing to cast to an array.
// OVERLOAD HIT: IEnumerable<string>
HashSet<string> prereqs2 = [.. runningPreReqs];
builder.Services.RegisterStep<StandardStep>("NamedStandard2", prereqs2);
runningPreReqs.Add("NamedStandard2");

#endregion

#region RegisterStepWithArgument

// Register the step with no alternate name and passing the tuple parameter as part of registration.
// Registration method will handle the creation of the step and its dependencies.
// Making prerequisites an explicit string array.
// OVERLOAD HIT: params string[]
string[] prereqs3 = [nameof(StandardStep), "NamedStandard1"];
builder.Services.RegisterStepWithArgument<ReusableStepWithSingleParam, (string, string)>(
    (value1, value2),
    prereqs3);
runningPreReqs.Add(nameof(ReusableStepWithSingleParam));

// Register the step with no alternate name and passing the tuple parameter as part of registration.
// Making prerequisites a List inline.
// OVERLOAD HIT: IEnumerable<string>
List<string> prereqs3b = [.. runningPreReqs];
builder.Services.RegisterStepWithArgument<ReusableStepWithSingleParam, (string, string)>(
    (value1, value2),
    prereqs3b);

#endregion

#region RegisterStepWithArgumentAndName

// Register the step with an alternate name and passing the tuple parameter as part of registration.
// Registration method will handle the creation of the step and its dependencies.
// Making prerequisites inline comma-separated strings.
// OVERLOAD HIT: params string[]
builder.Services.RegisterStepWithArgument<ReusableStepWithSingleParam, (string, string)>(
    "NamedArg1",
    (value1, value2),
    nameof(StandardStep), nameof(ReusableStepWithSingleParam));
runningPreReqs.Add("NamedArg1");

// Register the step with an alternate name and passing the tuple parameter as part of registration.
// Making prerequisites generated dynamically during the registration process using a LINQ query.
// OVERLOAD HIT: IEnumerable<string>
builder.Services.RegisterStepWithArgument<ReusableStepWithSingleParam, (string, string)>(
    "NamedArg2",
    (value1, value2),
    runningPreReqs.Where(x => x.Contains("Standard")));
runningPreReqs.Add("NamedArg2");

#endregion

#region RegisterStepWithArguments

// Register the step with no alternate name and passing the parameters to the registration method.
// Registration method does not care if parameters are at the beginning, middle, or end or even next to each other.
// It will handle the creation of the step and its dependencies and pass the parameters to the constructor in the correct order that they are passed in.
// Making prerequisites empty.
// OVERLOAD HIT: params string[]
builder.Services.RegisterStepWithArguments<ReusableStepWithMultipleParams>([value1, value2]);
runningPreReqs.Add(nameof(ReusableStepWithMultipleParams));

// Register the step with no alternate name and passing the parameters to the registration method.
// Registration method will handle the creation of the step and its dependencies.
// Making prerequisites a direct List reference.
// OVERLOAD HIT: IEnumerable<string>
builder.Services.RegisterStepWithArguments<ReusableStepWithMultipleParams>(
    [value1, value2],
    runningPreReqs);

#endregion

#region RegisterStepWithArgumentsAndName

// Register the step with an alternate name and passing the parameters to the registration method.
// Registration method will handle the creation of the step and its dependencies.
// Making prerequisites inline comma-separated values.
// OVERLOAD HIT: params string[]
builder.Services.RegisterStepWithArguments<ReusableStepWithMultipleParams>(
    "NamedMultiArgs1",
    [value1, value2],
    "NamedArg1", "NamedArg2");
runningPreReqs.Add("NamedMultiArgs1");

// Register the step with an alternate name and passing the parameters to the registration method.
// Making prerequisites an inline array cast to IEnumerable.
// OVERLOAD HIT: IEnumerable<string>
IEnumerable<string> prereqs6b = ["NamedMultiArgs1", nameof(ReusableStepWithMultipleParams)];
builder.Services.RegisterStepWithArguments<ReusableStepWithMultipleParams>(
    "NamedMultiArgs2",
    [value1, value2],
    prereqs6b);
runningPreReqs.Add("NamedMultiArgs2");

#endregion

#region RegisterStepImplementationFactory

// Register the step with no alternate name and passing the parameters directly to the constructor using the factory.
// Making prerequisite step a single parameter.
// OVERLOAD HIT: params string[]
builder.Services.RegisterStep<ReusableStepWithSingleParam>((sp, _) =>
{
    var memoryDS = sp.GetRequiredService<MemoryDataSource<SampleClass>>();
    var dataverseDataSource = sp.GetRequiredService<DataverseDataSource>();
    var reducer = sp.GetRequiredService<EntityOperationReducer>();
    var batchProcessor = sp.GetRequiredService<BatchProcessor>();
    return new ReusableStepWithSingleParam(memoryDS, dataverseDataSource, (value1, value2), reducer, batchProcessor);
}, nameof(StandardStep));

// Register the step with no alternate name and passing the parameters directly to the constructor.
// Making prerequisites generated dynamically using a LINQ Select query.
// OVERLOAD HIT: IEnumerable<string>
builder.Services.RegisterStep<ReusableStepWithSingleParam>((sp, _) =>
{
    var memoryDS = sp.GetRequiredService<MemoryDataSource<SampleClass>>();
    var dataverseDataSource = sp.GetRequiredService<DataverseDataSource>();
    var reducer = sp.GetRequiredService<EntityOperationReducer>();
    var batchProcessor = sp.GetRequiredService<BatchProcessor>();
    return new ReusableStepWithSingleParam(memoryDS, dataverseDataSource, (value1, value2), reducer, batchProcessor);
}, runningPreReqs.Select(x => x));

#endregion

#region RegisterStepImplementationFactoryAndName

// Register the step with an alternate name and passing the parameters directly to the constructor.
// Making prerequisites regular comma-separated params.
// OVERLOAD HIT: params string[]
builder.Services.RegisterStep<ReusableStepWithSingleParam>("NamedFactory1", (sp, _) =>
{
    var memoryDS = sp.GetRequiredService<MemoryDataSource<SampleClass>>();
    var dataverseDataSource = sp.GetRequiredService<DataverseDataSource>();
    var reducer = sp.GetRequiredService<EntityOperationReducer>();
    var batchProcessor = sp.GetRequiredService<BatchProcessor>();
    return new ReusableStepWithSingleParam(memoryDS, dataverseDataSource, (value1, value2), reducer, batchProcessor);
}, nameof(StandardStep), "NamedMultiArgs1");
runningPreReqs.Add("NamedFactory1");

// Register the step with an alternate name and passing the parameters directly to the constructor.
// Making prerequisites generated dynamically and filtering out bad data during the registration process.
// OVERLOAD HIT: IEnumerable<string>
runningPreReqs.Add("RandomStringThatWouldBreakThis");
builder.Services.RegisterStep<ReusableStepWithSingleParam>("NamedFactory2", (sp, _) =>
{
    var memoryDS = sp.GetRequiredService<MemoryDataSource<SampleClass>>();
    var dataverseDataSource = sp.GetRequiredService<DataverseDataSource>();
    var reducer = sp.GetRequiredService<EntityOperationReducer>();
    var batchProcessor = sp.GetRequiredService<BatchProcessor>();
    return new ReusableStepWithSingleParam(memoryDS, dataverseDataSource, (value1, value2), reducer, batchProcessor);
}, runningPreReqs.Where(x => !x.Contains("RandomStringThatWouldBreakThis", StringComparison.OrdinalIgnoreCase)));

#endregion

var host = builder.Build();
host.Start();
var runtime = host.Services.GetRequiredService<IIntegrationRuntime>();
await runtime.RunAllStepsAsync();
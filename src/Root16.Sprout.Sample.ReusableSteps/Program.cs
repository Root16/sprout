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
// to simulate a more complex scenario where the preregistration steps are generated dynamically
List<string> runningPreReqs = new();

#region RegisterStep & RegisterStepWithName

// Register the step with no alternate name and only getting dependency injection from the DI container
// Making prerequeisite step empty
// OVERLOAD HIT: params string[]
builder.Services.RegisterStep<StandardStep>();
runningPreReqs.Add(nameof(StandardStep));

// Register the step with an alternate name and only getting dependency injection from the DI container
// Making prequresities an IEnumerable to simulate a more complex scenario where the preregistration steps are generated dynamically
// OVERLOAD HIT: IEnumerable<string> 
IEnumerable<string> prereqs1 = [.. runningPreReqs];
builder.Services.RegisterStep<StandardStep>("NamedStandard1", prereqs1);
runningPreReqs.Add("NamedStandard1");

// Register the step with an alternate name and only getting dependency injection from the DI container
// Making prerequeisite step a regular params
// OVERLOAD HIT: params string[]
builder.Services.RegisterStep<StandardStep>("NamedStandard2", nameof(StandardStep), "NamedStandard1");
runningPreReqs.Add("NamedStandard2");

#endregion

#region RegisterStepWithArgument & RegisterStepWithArgumentAndName

// Register the step with no alternate name and passing the tuple parameter as part of registration.
// Registration method will handle the creation of the step and its dependencies.
// Making prerequeisite step empty
// OVERLOAD HIT: params string[]
builder.Services.RegisterStepWithArgument<ReusableStepWithSingleParam, (string, string)>(
    (value1, value2));
runningPreReqs.Add(nameof(ReusableStepWithSingleParam));

// Register the step with an alternate name and passing the tuple parameter as part of registration.
// Registration method will handle the creation of the step and its dependencies.
// Making prequresities a List inline
// OVERLOAD HIT: IEnumerable<string>
List<string> prereqs3b = [.. runningPreReqs];
builder.Services.RegisterStepWithArgument<ReusableStepWithSingleParam, (string, string)>(
    "NamedArg1",
    (value1, value2),
    prereqs3b);
runningPreReqs.Add("NamedArg1");

// Register the step with an alternate name and passing the tuple parameter as part of registration.
// Registration method will handle the creation of the step and its dependencies.
// Making prequresities a List/Array
// OVERLOAD HIT: params string[]
builder.Services.RegisterStepWithArgument<ReusableStepWithSingleParam, (string, string)>(
    "NamedArg2",
    (value1, value2),
    nameof(StandardStep), nameof(ReusableStepWithSingleParam));
runningPreReqs.Add("NamedArg2");

#endregion

#region RegisterStepWithArguments & RegisterStepWithArgumentsAndName

// Register the step with no alternate name and passing the parameters to the registration method.
// Registration method will handle the creation of the step and its dependencies.
// Registration method does not care if parameters are at the beginning, middle, or end or even next to each other.
// It will handle the creation of the step and its dependencies and pass the parameters to the constructor in the correct order that they are passed in
// Making prerequeisite step empty
// OVERLOAD HIT: params string[]
builder.Services.RegisterStepWithArguments<ReusableStepWithMultipleParams>([value1, value2]);
runningPreReqs.Add(nameof(ReusableStepWithMultipleParams));

// Register the step with an alternate name and passing the parameters to the registration method.
// Registration method will handle the creation of the step and its dependencies.
// Registration method does not care if parameters are at the beginning, middle, or end or even next to each other.
// It will handle the creation of the step and its dependencies and pass the parameters to the constructor in the correct order that they are passed in
// Making prequresities generated during the registration process
// OVERLOAD HIT: IEnumerable<string>
builder.Services.RegisterStepWithArguments<ReusableStepWithMultipleParams>(
    "NamedMultiArgs1",
    [value1, value2],
    runningPreReqs);
runningPreReqs.Add("NamedMultiArgs1");

// Register the step with an alternate name and passing the parameters to the registration method.
// Registration method will handle the creation of the step and its dependencies.
// Registration method does not care if parameters are at the beginning, middle, or end or even next to each other.
// It will handle the creation of the step and its dependencies and pass the parameters to the constructor in the correct order that they are passed in
// Making prerequeisite step a regular params
// OVERLOAD HIT: params string[]
builder.Services.RegisterStepWithArguments<ReusableStepWithMultipleParams>(
    "NamedMultiArgs2",
    [value1, value2],
    "NamedArg1", "NamedArg2");
runningPreReqs.Add("NamedMultiArgs2");

#endregion

#region RegisterStepImplementationFactory & RegisterStepImplementationFactoryAndName

// Register the step with an alternate name and passing the parameters directly to the constructor
// Making prequresities an IEnumerable to simulate a more complex scenario where the preregistration steps are generated dynamically
// OVERLOAD HIT: IEnumerable<string>
builder.Services.RegisterStep<ReusableStepWithSingleParam>("NamedFactory1", (sp, _) =>
{
    var memoryDS = sp.GetRequiredService<MemoryDataSource<SampleClass>>();
    var dataverseDataSource = sp.GetRequiredService<DataverseDataSource>();
    var reducer = sp.GetRequiredService<EntityOperationReducer>();
    var batchProcessor = sp.GetRequiredService<BatchProcessor>();
    return new ReusableStepWithSingleParam(memoryDS, dataverseDataSource, (value1, value2), reducer, batchProcessor);
}, runningPreReqs.Select(x => x));
runningPreReqs.Add("NamedFactory1");

// Register the step with an alternate name and passing the parameters directly to the constructor
// Making prerequeisite step a regular params
// OVERLOAD HIT: params string[]
builder.Services.RegisterStep<ReusableStepWithSingleParam>("NamedFactory2", (sp, _) =>
{
    var memoryDS = sp.GetRequiredService<MemoryDataSource<SampleClass>>();
    var dataverseDataSource = sp.GetRequiredService<DataverseDataSource>();
    var reducer = sp.GetRequiredService<EntityOperationReducer>();
    var batchProcessor = sp.GetRequiredService<BatchProcessor>();
    return new ReusableStepWithSingleParam(memoryDS, dataverseDataSource, (value1, value2), reducer, batchProcessor);
}, nameof(StandardStep), "NamedMultiArgs1");

#endregion

var host = builder.Build();
host.Start();
var runtime = host.Services.GetRequiredService<IIntegrationRuntime>();
await runtime.RunAllStepsAsync();
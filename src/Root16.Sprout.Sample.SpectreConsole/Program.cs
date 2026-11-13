using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Root16.Sprout;
using Root16.Sprout.Sample.SpectreConsole;
using Root16.Sprout.SpectreConsole;


var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

builder.Logging.ClearProviders();

// Want to make sure that the steps take a little bit to run so we can see the console in action
builder.Services.AddSproutWithBatchDelay(TimeSpan.FromSeconds(1));
builder.Services.AddSpectreConsole();

// Hide Logs Below Warning for Dataverse connections
builder.Logging.AddFilter("Microsoft.PowerPlatform.Dataverse", LogLevel.Warning);

// Level 1: Base Steps (No Prerequisites)
builder.Services.RegisterStep<ReusableStepForSpectreConsole>("Step1");
builder.Services.RegisterStep<ReusableStepForSpectreConsole>("Step2");
builder.Services.RegisterStep<ReusableStepForSpectreConsole>("Step3");

// Level 2: Intermediate Steps (Depend on Level 1)
builder.Services.RegisterStep<ReusableStepForSpectreConsole>("Step4", "Step1");
builder.Services.RegisterStep<ReusableStepForSpectreConsole>("Step5", "Step1", "Step2");
builder.Services.RegisterStep<ReusableStepForSpectreConsole>("Step6", "Step3");
builder.Services.RegisterStep<ReusableStepForSpectreConsole>("Step7", "Step2");

// Level 3: Final Steps (Depend on Level 2)
builder.Services.RegisterStep<ReusableStepForSpectreConsole>("Step8", "Step4");
builder.Services.RegisterStep<ReusableStepForSpectreConsole>("Step9", "Step4", "Step5");
builder.Services.RegisterStep<ReusableStepForSpectreConsole>("Step10", "Step5");
builder.Services.RegisterStep<ReusableStepForSpectreConsole>("Step11", "Step6");
builder.Services.RegisterStep<ReusableStepForSpectreConsole>("Step12", "Step7");

var host = builder.Build();
host.Start();

var runtime = host.Services.GetRequiredService<IIntegrationRuntime>();
await runtime.RunAllStepsAsync(2);
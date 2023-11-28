using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Xrm.Sdk;
using Root16.Sprout;
using Root16.Sprout.Extensions;
using Root16.Sprout.Processors;
using Root16.Sprout.Progress;
using Root16.Sprout.Sample;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<IIntegrationRuntime, IntegrationRuntime>();
builder.Services.AddTransient<BatchProcessBuilder>();
builder.Services.AddTransient<BatchProcessor<Contact,Entity>>();
builder.Services.AddSingleton<IProgressListener, ConsoleProgressListener>();
builder.Services.AddStep<TestStep>();
var host = builder.Build();
host.Start();

var runtime = host.Services.GetRequiredService<IIntegrationRuntime>();
await runtime.RunAllStepsAsync();
using Microsoft.Extensions.Hosting;
using Root16.Sprout.Extensions;
using Root16.Sprout.Sample;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddStep<TestStep>();
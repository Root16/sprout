using Microsoft.Extensions.DependencyInjection;
using Root16.Sprout.Step;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Root16.Sprout.DependencyInjection
{
    public class StepRunner
    {
        private readonly IEnumerable<StepRegistration> stepRegistrations;
        private readonly IServiceScopeFactory serviceScopeFactory;

        public StepRunner(IEnumerable<StepRegistration> stepRegistrations, IServiceScopeFactory serviceScopeFactory)
        {
            this.stepRegistrations = stepRegistrations;
            this.serviceScopeFactory = serviceScopeFactory;
        }

        public async Task RunStepAsync(string name)
        {
            var reg = stepRegistrations.FirstOrDefault(step => step.Name == name);
            if (reg == null) throw new InvalidCastException($"Step named '{name}' is not registered.");
            
            using var scope = serviceScopeFactory.CreateScope();
            var step = (IIntegrationStep)scope.ServiceProvider.GetRequiredService(reg.StepType);
            await step.RunAsync();
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Root16.Sprout.Step;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Root16.Sprout.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddStep<TStep>(this IServiceCollection services)
            where TStep : class, IIntegrationStep
        {
            services.AddSingleton(new StepRegistration(typeof(TStep)));
            services.AddTransient<TStep>();

            return services;
        }

        public static IServiceCollection AddStep<TStep>(this IServiceCollection services, string name)
            where TStep : class, IIntegrationStep
        {
            services.AddSingleton(new StepRegistration(typeof(TStep), name));
            services.AddTransient<TStep>();

            return services;
        }
    }
}

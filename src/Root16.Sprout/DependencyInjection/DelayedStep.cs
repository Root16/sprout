namespace Root16.Sprout.DependencyInjection;

internal delegate Task<string> AsyncStepRunner(StepRegistration stepRegistration, Action<IIntegrationStep>? stepConfigurator = null);

record DelayedStep(StepRegistration StepRegistration, AsyncStepRunner StepRunner)
{

}

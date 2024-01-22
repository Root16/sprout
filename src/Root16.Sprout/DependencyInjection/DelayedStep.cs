namespace Root16.Sprout.DependencyInjection;

internal delegate Task<string> AsyncStepRunner(StepRegistration stepRegistration);

record DelayedStep(StepRegistration StepRegistration, AsyncStepRunner StepRunner)
{

}

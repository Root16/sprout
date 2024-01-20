using Root16.Sprout.DependencyInjection;

namespace Root16.Sprout;

internal delegate Task<string> AsyncStepRunner(StepRegistration stepRegistration);

record DelayedStep(StepRegistration StepRegistration, AsyncStepRunner StepRunner)
{

}

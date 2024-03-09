namespace Root16.Sprout.DependencyInjection;

public class StepRegistrationDependencyList(params Type[] steps) : List<Type>(steps)
{
}

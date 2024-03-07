namespace Root16.Sprout.DependencyInjection;

public class StepRegistrationDependencyList : List<Type>
{
    public StepRegistrationDependencyList(params Type[] steps) : base(steps)
    {
    }
}

namespace Root16.Sprout.DependencyInjection;

public class StepRegistration
{
    public Type StepType { get; }
    public string Name { get; }
    public List<string> DependentSteps { get; } = new List<string>();

    public StepRegistration(Type stepType, List<string>? dependentSteps = null) 
    {
        StepType = stepType;
        Name = stepType.Name;
        if(dependentSteps != null)
        {
            DependentSteps = dependentSteps;
        }
    }
}
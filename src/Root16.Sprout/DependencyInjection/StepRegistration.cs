namespace Root16.Sprout.DependencyInjection;

public class StepRegistration
{
    public Type StepType { get; }
    public string Name { get; }
    public List<string> PrerequisteSteps { get; } = new List<string>();

    public StepRegistration(Type stepType, List<string>? prerequisteSteps = null) 
    {
        StepType = stepType;
        Name = stepType.Name;
        if(prerequisteSteps != null)
        {
            PrerequisteSteps = prerequisteSteps;
        }
    }
}
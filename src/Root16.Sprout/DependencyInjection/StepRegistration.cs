namespace Root16.Sprout.DependencyInjection;

public class StepRegistration
{
    public Type StepType { get; }
    public string Name { get; }
    public List<string> PrerequisteSteps { get; } = [];
    internal HashSet<string> DependentSteps { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public StepRegistration(Type stepType, List<string>? prerequisteSteps = default) 
    {
        StepType = stepType;
        Name = stepType.Name;
        if(prerequisteSteps is not null)
        {
            PrerequisteSteps = prerequisteSteps;
        }
    }

    public StepRegistration(Type stepType, string name, List<string>? prerequisteSteps = default)
    {
        StepType = stepType;
        Name = name;
        if(prerequisteSteps is not null)
        {
            PrerequisteSteps = prerequisteSteps;
        }
    }

}
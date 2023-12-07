namespace Root16.Sprout.DependencyInjection;

public class StepRegistration
{
    public Type StepType { get; }
    public string Name { get; }

    public StepRegistration(Type stepType)
    {
        StepType = stepType;
        Name = stepType.Name;
    }

    public StepRegistration(Type stepType, string name)
    {
        StepType = stepType;
        Name = name;
    }
}
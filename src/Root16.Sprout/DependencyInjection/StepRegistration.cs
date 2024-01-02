using System.Diagnostics.CodeAnalysis;

namespace Root16.Sprout.DependencyInjection;

public class StepRegistration
{
    public Type StepType { get; }
    public string Name { get; }

    public StepRegistration(Type stepType, string? name = null)
    {
        StepType = stepType;
        Name = name is null ? stepType.Name : name;
    }
}
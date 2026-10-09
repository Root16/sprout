namespace Root16.Sprout;

public interface IIntegrationStep
{
	Task RunAsync(string stepName);
}


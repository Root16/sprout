namespace Root16.Sprout.Progress;

public interface IProgressListener
{
	void OnRunStart(IIntegrationRuntime runtime);
	void OnStepStart(IIntegrationRuntime runtime, string name);
	void OnProgressChange(IIntegrationRuntime runtime, IntegrationProgress progress);
	void OnStepComplete(IIntegrationRuntime runtime, string name);
	void OnRunComplete(IIntegrationRuntime runtime);
}

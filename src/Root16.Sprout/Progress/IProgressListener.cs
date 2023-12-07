namespace Root16.Sprout.Progress;

public interface IProgressListener
{
	void OnRunStart();
	void OnStepStart(string name);
	void OnProgressChange(IntegrationProgress progress);
	void OnStepComplete(string name);
	void OnRunComplete();
}

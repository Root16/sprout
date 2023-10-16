namespace Root16.Sprout.Progress;

public interface IProgressListener
{
	void OnRunStart(IMigrationRuntime runtime);
	void OnStepStart(IMigrationRuntime runtime, string name);
	void OnProgressChange(IMigrationRuntime runtime, MigrationProgress progress);
	void OnStepComplete(IMigrationRuntime runtime, string name);
	void OnRunComplete(IMigrationRuntime runtime);
}

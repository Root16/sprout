namespace Root16.Sprout.Progress;

public interface IProgressListener
{
	Task OnRunStart(IList<string> stepNames);
	Task OnStepStart(string name);
	Task OnProgressChange(IntegrationProgress progress);
	Task OnStepComplete(string name);
	Task OnRunComplete();
}

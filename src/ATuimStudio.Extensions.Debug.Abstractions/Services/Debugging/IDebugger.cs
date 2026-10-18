namespace ATuimStudio.Extensions.Debug
{
	public interface IDebugger : IDisposable
	{
		Task StartAsync(CancellationToken cancellationToken);
		Task ContinueAsync(CancellationToken cancellationToken);
		Task StepInAsync(CancellationToken cancellationToken);
		Task StepOverAsync(CancellationToken cancellationToken);
		Task StepOutAsync(CancellationToken cancellationToken);
		Task TerminateAsync(CancellationToken cancellationToken);
		Task<bool> ToggleBreakpointAsync(string sourceFilePath, SourcePosition position, CancellationToken cancellationToken);
		event EventHandler<BreakpointEventArgs>? OnStarted;
		event EventHandler<BreakpointEventArgs>? OnBreakpoint;
		event EventHandler<BreakpointEventArgs>? OnContinued;
		event EventHandler<TerminatedEventArgs>? OnTerminated;
		event EventHandler<string>? OnStandardOut;
		event EventHandler<string>? OnStandardError;
		StreamWriter StandardInput { get; }
	}
}

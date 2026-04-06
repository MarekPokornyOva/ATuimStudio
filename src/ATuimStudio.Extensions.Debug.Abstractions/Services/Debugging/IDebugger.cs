namespace ATuimStudio.Extensions.Debug
{
	public interface IDebugger : IDisposable
	{
		void Start();
		void Continue();
		void StepIn();
		void StepOver();
		void StepOut();
		void Terminate();
		bool ToggleBreakpoint(string sourceFilePath, SourcePosition position);
		event EventHandler<BreakpointEventArgs>? OnStarted;
		event EventHandler<BreakpointEventArgs>? OnBreakpoint;
		event EventHandler<BreakpointEventArgs>? OnContinued;
		event EventHandler<TerminatedEventArgs>? OnTerminated;
		event EventHandler<string>? OnStandardOut;
		event EventHandler<string>? OnStandardError;
		StreamWriter StandardInput { get; }
	}
}

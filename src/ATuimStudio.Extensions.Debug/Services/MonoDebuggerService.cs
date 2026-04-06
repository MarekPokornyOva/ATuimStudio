namespace ATuimStudio.Extensions.Debug
{
	sealed class MonoDebuggerService : IDebuggerService, IDebuggerProvider
	{
		public IDebugger? Current { get; private set; }

		public event EventHandler<EventArgs>? OnChanged;

		public Task<IDebugger> CreateDebuggerAsync(string path, string? arguments, IEnumerable<KeyValuePair<string, string>>? environmentVariables, CancellationToken cancellationToken)
		{
			MonoDebugger debugger = new MonoDebugger(path, arguments, environmentVariables);
			Current = debugger;
			OnChanged?.Invoke(debugger, EventArgs.Empty);
			debugger.OnTerminated += DebuggerOnTerminated;
			return Task.FromResult<IDebugger>(debugger);
		}

		void DebuggerOnTerminated(object? sender, TerminatedEventArgs e)
		{
			((MonoDebugger)sender!).OnTerminated -= DebuggerOnTerminated;
			Current = null;
			OnChanged?.Invoke(null!, EventArgs.Empty);
		}
	}
}

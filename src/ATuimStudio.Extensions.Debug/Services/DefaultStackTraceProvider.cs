namespace ATuimStudio.Extensions.Debug
{
	sealed class DefaultStackTraceProvider : IStackTraceProvider, IDisposable
	{
		readonly IDebuggerProvider _debuggerProvider;
		public DefaultStackTraceProvider(IDebuggerProvider debuggerProvider)
		{
			_debuggerProvider = debuggerProvider;

			debuggerProvider.OnChanged += DebuggerChanged;
			DebuggerChanged(_debuggerProvider.Current, EventArgs.Empty);
		}

		public void Dispose()
		{
			Disconnect();
			_debuggerProvider.OnChanged -= DebuggerChanged;
		}

		void Disconnect()
		{
			if (_lastDebugger != null)
			{
				_lastDebugger.OnBreakpoint -= DebuggerBreakpoint;
				_lastDebugger.OnContinued -= DebuggerContinued;
			}
		}

		IDebugger? _lastDebugger;
		void DebuggerChanged(object? sender, EventArgs e)
		{
			Disconnect();

			IDebugger? debugger = (IDebugger?)sender;
			if (debugger != null)
			{
				debugger.OnBreakpoint += DebuggerBreakpoint;
				debugger.OnContinued += DebuggerContinued;
			}

			_lastDebugger = debugger;
		}

		void DebuggerBreakpoint(object? sender, BreakpointEventArgs e)
		{
			IReadOnlyList<IStackFrame> callStack = e.CallStack;
			CallStack = callStack;
			if (callStack.Count != 0)
				SelectedFrame = callStack[0];
		}

		void DebuggerContinued(object? sender, BreakpointEventArgs e)
		{
			CallStack = null;
			SelectedFrame = null;
		}

		public IReadOnlyList<IStackFrame>? CallStack { get; private set { field = value; OnCallStackChanged?.Invoke(this, EventArgs.Empty); } }

		public IStackFrame? SelectedFrame { get; set { field = value; OnSelectedFrameChanged?.Invoke(this, EventArgs.Empty); } }

		public event EventHandler<EventArgs>? OnCallStackChanged;
		public event EventHandler<EventArgs>? OnSelectedFrameChanged;
	}
}

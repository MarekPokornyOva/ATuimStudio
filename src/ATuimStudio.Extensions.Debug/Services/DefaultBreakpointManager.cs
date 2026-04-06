namespace ATuimStudio.Extensions.Debug
{
	class DefaultBreakpointManager : IBreakpointManager
	{
		readonly IDebuggerProvider _debuggerProvider;
		public DefaultBreakpointManager(IDebuggerProvider debuggerProvider)
		{
			_debuggerProvider = debuggerProvider;
		}

		readonly List<Breakpoint> _breakpoints = new List<Breakpoint>();

		public IEnumerable<Breakpoint> Breakpoints => _breakpoints;

		public bool ToggleBreakpoint(string filepath, int line, int col)
		{
			IDebugger? debugger = _debuggerProvider.Current;
			if (debugger != null)
				if (!debugger.ToggleBreakpoint(filepath, new SourcePosition(line, col)))
					return false;

			int existingIndex = _breakpoints.FindIndex(x => x.Filepath.EqualsOrdinal(filepath) && x.Range.Start.Line == line);
			Breakpoint bp;
			if (existingIndex == -1)
			{
				_breakpoints.Add(bp = new Breakpoint(filepath, new SourceRange(line, col, line, col)));
				BreakpointAdded?.Invoke(this, bp);
			}
			else
			{
				bp = _breakpoints[existingIndex];
				_breakpoints.RemoveAt(existingIndex);
				BreakpointRemoved?.Invoke(this, bp);
			}
			return true;
		}

		public event EventHandler<Breakpoint>? BreakpointAdded;
		public event EventHandler<Breakpoint>? BreakpointRemoved;
	}
}

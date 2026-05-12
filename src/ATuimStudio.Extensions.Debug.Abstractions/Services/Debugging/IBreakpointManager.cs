namespace ATuimStudio.Extensions.Debug
{
	public interface IBreakpointManager
	{
		bool ToggleBreakpoint(string filepath, int line, int col);
		IReadOnlyCollection<Breakpoint> Breakpoints { get; }

		event EventHandler<Breakpoint> BreakpointAdded;
		event EventHandler<Breakpoint> BreakpointRemoved;
	}

	public record struct Breakpoint(string Filepath, SourceRange Range);
}

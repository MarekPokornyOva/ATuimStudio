using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace ATuimStudio.Extensions.Debug;

public sealed partial class DebugBreakpointsViewModel : ObservableObject, IDisposable
{
	public ObservableCollection<Breakpoint> Breakpoints { get; } = [];

	readonly IBreakpointManager _breakpointManager;
	public DebugBreakpointsViewModel(IBreakpointManager breakpointManager)
	{
		_breakpointManager = breakpointManager;
		RefreshBreakpoints();
		breakpointManager.BreakpointAdded += RefreshBreakpointsHandler;
		breakpointManager.BreakpointRemoved += RefreshBreakpointsHandler;
	}

	public void Dispose()
	{
		_breakpointManager.BreakpointAdded -= RefreshBreakpointsHandler;
		_breakpointManager.BreakpointRemoved -= RefreshBreakpointsHandler;
	}

	void RefreshBreakpointsHandler(object? sender, Breakpoint e)
		=> RefreshBreakpoints();

	void RefreshBreakpoints()
	{
		Breakpoints.Clear();
		Breakpoints.AddRange(_breakpointManager.Breakpoints);
	}
}

using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;

namespace ATuimStudio.Extensions.Debug;

public sealed partial class DebugCallStackViewModel : Tool, IDisposable
{
	[ObservableProperty]
	IReadOnlyList<IStackFrame>? _callStack;

	[ObservableProperty]
	private IStackFrame? _selectedFrame;

	readonly IStackTraceProvider _stackTraceProvider;
	public DebugCallStackViewModel(IStackTraceProvider stackTraceProvider)
	{
		_stackTraceProvider = stackTraceProvider;
		stackTraceProvider.OnCallStackChanged += CallStackChanged;
		stackTraceProvider.OnSelectedFrameChanged += OutsideSelectedFrameChanged;
	}

	public void Dispose()
	{
		_stackTraceProvider.OnCallStackChanged -= CallStackChanged;
		_stackTraceProvider.OnSelectedFrameChanged -= OutsideSelectedFrameChanged;
	}

	void CallStackChanged(object? sender, EventArgs e)
	{
		CallStack = ((IStackTraceProvider?)sender)?.CallStack;
	}

	void OutsideSelectedFrameChanged(object? sender, EventArgs e)
	{
		SelectedFrame = ((IStackTraceProvider?)sender)?.SelectedFrame;
	}

	partial void OnSelectedFrameChanged(IStackFrame? value)
	{
		_stackTraceProvider.SelectedFrame = value;
	}
}

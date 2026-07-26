using ATuimStudio.Extensions.Core.Ui;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATuimStudio.Extensions.Debug;

public sealed partial class DebugCallStackViewModel : ObservableObject, IDisposable
{
	[ObservableProperty]
	IReadOnlyList<IStackFrame>? _callStack;

	[ObservableProperty]
	private IStackFrame? _selectedFrame;

	readonly IStackTraceProvider _stackTraceProvider;
	readonly IUiDocumentService _documentService;
	public DebugCallStackViewModel(IStackTraceProvider stackTraceProvider, IUiDocumentService documentService)
	{
		_stackTraceProvider = stackTraceProvider;
		_documentService = documentService;

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
		IStackFrame? selectedFrame = ((IStackTraceProvider?)sender)?.SelectedFrame;
		SelectedFrame = selectedFrame;

		if (selectedFrame != null)
		{
			SourceRange? pos = selectedFrame.Range;
			if (pos.HasValue)
			{
				SourcePosition start = pos.Value.Start;
				_documentService.SetActiveDocument(selectedFrame.SourceFilePath, start.Line, start.Column);
			}
			else
				_documentService.SetActiveDocument(selectedFrame.SourceFilePath);
		}
	}

	partial void OnSelectedFrameChanged(IStackFrame? value)
	{
		_stackTraceProvider.SelectedFrame = value;
	}
}

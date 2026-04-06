namespace ATuimStudio.Extensions.Debug
{
	public interface IStackTraceProvider
	{
		IReadOnlyList<IStackFrame>? CallStack { get; }
		event EventHandler<EventArgs>? OnCallStackChanged;
		IStackFrame? SelectedFrame { get; set; }
		event EventHandler<EventArgs>? OnSelectedFrameChanged;
	}
}

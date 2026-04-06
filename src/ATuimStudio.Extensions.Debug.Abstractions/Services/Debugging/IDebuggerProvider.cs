namespace ATuimStudio.Extensions.Debug
{
	public interface IDebuggerProvider
	{
		IDebugger? Current { get; }
		event EventHandler<EventArgs>? OnChanged;
	}
}

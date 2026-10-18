namespace ATuimStudio.Extensions.Debug
{
	public interface IDebuggerProvider
	{
		IDebugger? Current { get; set; }
		event EventHandler<EventArgs>? OnChanged;
	}
}

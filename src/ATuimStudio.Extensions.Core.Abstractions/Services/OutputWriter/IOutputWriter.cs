namespace ATuimStudio.Extensions.Core
{
	public interface IOutputWriter
	{
		void Log(string type, OutputWriterSeverity severity, string message);
		event EventHandler<OutputWriterLoggedEventArgs> Logged;
	}

	public enum OutputWriterSeverity
	{
		Info,
		Warning,
		Error
	}

	public sealed record OutputWriterLoggedEventArgs(string Type, OutputWriterSeverity Severity, string Message);
}

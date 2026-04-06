namespace ATuimStudio.Extensions.Core
{
	sealed class DefaultOutputWriter : IOutputWriter
	{
		public event EventHandler<OutputWriterLoggedEventArgs>? Logged;

		public void Log(string type, OutputWriterSeverity severity, string message)
			=> Logged?.Invoke(null, new OutputWriterLoggedEventArgs(type, severity, message));
	}
}

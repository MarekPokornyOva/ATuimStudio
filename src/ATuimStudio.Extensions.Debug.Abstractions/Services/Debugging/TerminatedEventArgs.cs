namespace ATuimStudio.Extensions.Debug
{
	public sealed class TerminatedEventArgs : EventArgs
	{
		public static TerminatedEventArgs Instance { get; } = new TerminatedEventArgs();

		private TerminatedEventArgs()
		{ }
	}
}

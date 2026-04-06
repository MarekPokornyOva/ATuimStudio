namespace ATuimStudio.Extensions.Core
{
	public sealed class SolutionUnloadedEventArgs : EventArgs
	{
		public static SolutionUnloadedEventArgs Instance { get; } = new SolutionUnloadedEventArgs();
		private SolutionUnloadedEventArgs()
		{ }
	}
}

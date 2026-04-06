namespace ATuimStudio.Extensions.Core
{
	public sealed class SolutionLoadedEventArgs : EventArgs
	{
		public SolutionLoadedEventArgs(ISolutionData solution)
			=> Solution = solution;

		public ISolutionData Solution { get; }
	}
}

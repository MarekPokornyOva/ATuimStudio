namespace ATuimStudio.Extensions.Core
{
	public sealed class SolutionUnloadingEventArgs : EventArgs
	{
		public SolutionUnloadingEventArgs(ISolutionData solution)
			=> Solution = solution;

		public ISolutionData Solution { get; }
	}
}

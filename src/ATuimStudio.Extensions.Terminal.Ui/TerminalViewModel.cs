using ATuimStudio.Extensions.Core;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATuimStudio.Extensions.Terminal;

sealed partial class TerminalViewModel : ObservableObject
{
	[ObservableProperty]
	public partial string? SolutionDirectory { get; set; }

	public TerminalViewModel(ISolutionService solutionService)
	{
		SolutionDirectory = solutionService.CurrentSolution?.Path;

		solutionService.OnSolutionLoaded += (sender, e) => SolutionChanged(e.Solution);
		solutionService.OnSolutionUnloaded += (sender, e) => SolutionChanged(null);
	}

	void SolutionChanged(ISolutionData? solutionData)
	{
		Dispatcher.UIThread.Post(() =>
			SolutionDirectory = solutionData?.Path
			);
	}
}

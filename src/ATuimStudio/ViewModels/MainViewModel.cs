using ATuimStudio.Extensions.Core;
using ATuimStudio.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Controls;
using System.ComponentModel;

namespace ATuimStudio.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
	[ObservableProperty]
	IRootDock? _layout;

	const string _appName = "ATuimStudio";
	[ObservableProperty]
	string _windowTitle = _appName;

	[EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable CS8618
	public MainViewModel()
	{
	}
#pragma warning restore CS8618

	readonly DockFactory _dockFactory;
	internal readonly IPluginPartsRegistrator _pluginPartsRegistrator;
	public MainViewModel(DockFactory dockFactory, ISolutionService solutionService, IPluginPartsRegistrator pluginPartsRegistrator) : this()
	{
		_pluginPartsRegistrator = pluginPartsRegistrator;
		_dockFactory = dockFactory;

		dockFactory.LayoutRecreateRequested += DockFactory_LayoutRecreateRequested;
		dockFactory.InitializeLayouts();

		solutionService.OnSolutionLoaded += SolutionService_OnSolutionLoaded;
		solutionService.OnSolutionUnloaded += SolutionService_OnSolutionUnloaded;
	}

	private void DockFactory_LayoutRecreateRequested(object? sender, EventArgs e)
	{
		IRootDock layout = _dockFactory.CreateLayout();
		_dockFactory.InitLayout(layout);
		Layout = layout;
	}

	void SolutionService_OnSolutionLoaded(object? sender, SolutionLoadedEventArgs e)
		=> WindowTitle = $"{_appName} - {e.Solution.Name}";

	void SolutionService_OnSolutionUnloaded(object? sender, SolutionUnloadedEventArgs e)
		=> WindowTitle = _appName;
}

using ATuimStudio.Extensions.Core;
using ATuimStudio.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

	readonly ISolutionService _solutionService;
	readonly DockFactory _dockFactory;
	readonly ITopLevelVisualProvider _topLevelVisualProvider;
	internal readonly IPluginPartsRegistrator _pluginPartsRegistrator;
	public MainViewModel(DockFactory dockFactory, ISolutionService solutionService, IPluginPartsRegistrator pluginPartsRegistrator, ITopLevelVisualProvider topLevelVisualProvider) : this()
	{
		_solutionService = solutionService;
		_pluginPartsRegistrator = pluginPartsRegistrator;
		_topLevelVisualProvider = topLevelVisualProvider;
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

	[RelayCommand]
	async Task OpenSolution()
	{
		TopLevel? topLevel = TopLevel.GetTopLevel(_topLevelVisualProvider.Visual);
		if (topLevel == null)
			return;
		IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = "Choose solution",
			AllowMultiple = false,
			FileTypeFilter = [new FilePickerFileType("VS Solution") { Patterns = ["*.sln", "*.slnx"] }]
		});
		if (files.Count == 1)
		{
			string? filePath = files[0].TryGetLocalPath();
			if (filePath != null)
				await _solutionService.LoadSolutionAsync(filePath, CancellationToken.None);
		}
	}

	[RelayCommand]
	Task SaveDocument()
		=> _dockFactory.SaveCurrentDocument(CancellationToken.None);

	[RelayCommand]
	Task SaveAll()
		=> _dockFactory.SaveAllOpenedDocuments(CancellationToken.None);

	[RelayCommand]
	static void ExitApplication()
	{
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
			desktopApp.Shutdown(); // Gracefully shuts down the application
	}
}

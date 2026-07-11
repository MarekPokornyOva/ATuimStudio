using ATuimStudio.Extensibility;
using ATuimStudio.Extensions.Core;
using ATuimStudio.Extensions.Core.Ui;
using ATuimStudio.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio
{
	sealed class SystemExtension : ATuimStudio.Extensibility.UiExtension
	{
		const string OpenSolutionCommandCode = "OpenSolution";
		const string SaveDocumentCommandCode = "SaveDocument";
		const string SaveAllCommandCode = "SaveAll";
		const string ExitCommandCode = "Exit";
		const string UserOptionsCommandCode = "UserOptions";

		public override void RegisterCommand(ICommandRegistrator commandRegistrator)
		{
			DockFactory dockFactory = commandRegistrator.ServiceProvider.GetRequiredService<DockFactory>();
			ISolutionService solutionService = commandRegistrator.ServiceProvider.GetRequiredService<ISolutionService>();
			ITopLevelVisualProvider topLevelVisualProvider = commandRegistrator.ServiceProvider.GetRequiredService<ITopLevelVisualProvider>();
			IDialogService dialogService = commandRegistrator.ServiceProvider.GetRequiredService<IDialogService>();

			commandRegistrator.Register(OpenSolutionCommandCode, new AsyncRelayCommand(() => OpenSolution(solutionService, topLevelVisualProvider)), null);
			commandRegistrator.Register(SaveDocumentCommandCode, new AsyncRelayCommand(() => SaveDocument(dockFactory)), () => AssetLoader.Open(new Uri("avares://ATuimStudio/Assets/SaveDocument.png")));
			commandRegistrator.Register(SaveAllCommandCode, new AsyncRelayCommand(() => SaveAll(dockFactory)), () => AssetLoader.Open(new Uri("avares://ATuimStudio/Assets/SaveAll.png")));
			commandRegistrator.Register(ExitCommandCode, new RelayCommand(ExitApplication), null);
			commandRegistrator.Register(UserOptionsCommandCode, new RelayCommand(() => Options(dialogService)), null);
		}

		public override void RegisterMenu(IMenuRegistrator menuRegistrator)
		{
			menuRegistrator.Register([("_File", 100), ("_Open solution...", 10)], OpenSolutionCommandCode, null);
			menuRegistrator.Register([("_File", 100), ("_Save", 20)], SaveDocumentCommandCode, new KeyGesture(Key.S, KeyModifiers.Control));
			menuRegistrator.Register([("_File", 100), ("Save _All", 30)], SaveAllCommandCode, new KeyGesture(Key.S, KeyModifiers.Control | KeyModifiers.Shift));
			menuRegistrator.Register([("_File", 100), ("_Exit", 40)], ExitCommandCode, null);

			menuRegistrator.Register([("_Tools", 1000), ("_Options", 10)], UserOptionsCommandCode, null);
		}
		
		static async Task OpenSolution(ISolutionService solutionService, ITopLevelVisualProvider topLevelVisualProvider)
		{
			TopLevel? topLevel = TopLevel.GetTopLevel(topLevelVisualProvider.Visual);
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
					await solutionService.LoadSolutionAsync(filePath, CancellationToken.None);
			}
		}

		static Task SaveDocument(DockFactory dockFactory)
			=> dockFactory.SaveCurrentDocument(CancellationToken.None);

		static Task SaveAll(DockFactory dockFactory)
			=> dockFactory.SaveAllOpenedDocuments(CancellationToken.None);

		static void ExitApplication()
		{
			if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
				desktopApp.Shutdown(); // Gracefully shuts down the application
		}

		static Task Options(IDialogService dialogService)
			=> dialogService.OpenModal<OptionsDialogViewModel>(new DialogWindowParameters("Options"), []);
	}
}

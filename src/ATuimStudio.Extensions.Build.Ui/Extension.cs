using ATuimStudio.Extensibility;
using ATuimStudio.Extensions.Core;
using ATuimStudio.Extensions.Core.Ui;
using Avalonia.Input;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace ATuimStudio.Extensions.Build
{
	public sealed class Extension : ATuimStudio.Extensibility.UiExtension
	{
		const string CommandCode = "Build";
		const string OutputType = "Build";
		const string NoDebugStartCommandCode = "NoDebugStart";
		public override void RegisterCommand(ICommandRegistrator commandRegistrator)
		{
			async Task<string?> Build(CancellationToken cancellationToken)
			{
				IServiceProvider sp = commandRegistrator.ServiceProvider;
				IOutputWriter outputWriter = sp.GetRequiredService<IOutputWriter>();
				IUiDocumentService uiDocumentService = sp.GetRequiredService<IUiDocumentService>();

				await uiDocumentService.SaveAllOpenedDocuments(cancellationToken);

				IProjectInfo? proj = uiDocumentService.GetActiveDocumentProject();
				if (proj == null)
				{
					outputWriter.Log(OutputType, OutputWriterSeverity.Info, $"No project selected");
					return null;
				}
				IBuildResult result = await sp.GetRequiredService<IBuildService>()
					.BuildProjectAsync(proj.Name, CancellationToken.None);
				if (result.Success)
					outputWriter.Log(OutputType, OutputWriterSeverity.Info, $"Project {proj.Name} built sucessfully");
				else
					foreach (IBuildResultItem resItem in result.Items)
						outputWriter.Log(OutputType, resItem.Severity switch { BuildResultItemSeverity.Error => OutputWriterSeverity.Error, BuildResultItemSeverity.Warning => OutputWriterSeverity.Warning, _ => OutputWriterSeverity.Info }, $"{resItem.Location.Path}[{resItem.Location.Line}:{resItem.Location.Character}]: {resItem.Code}:{resItem.Message}");
				return result.Success ? result.OutputPath : null;
			}

			commandRegistrator.Register(CommandCode, new AsyncRelayCommand(Build), null);

			commandRegistrator.Register(NoDebugStartCommandCode, new AsyncRelayCommand(async (cancellationToken) =>
				{
					string? outputPath = await Build(cancellationToken);
					if (outputPath == null)
						return;
					using (Process process = Process.Start(new ProcessStartInfo(outputPath) { WorkingDirectory = Path.GetDirectoryName(outputPath) })!)
					{ }
				}),
				() => AssetLoader.Open(new Uri("avares://ATuimStudio.Extensions.Build.Ui/Assets/NoDebugStart.png"))
			);
		}

		public override void RegisterMenu(IMenuRegistrator menuRegistrator)
		{
			menuRegistrator.Register(["Build", "Build"], CommandCode, null);
			menuRegistrator.Register(["Debug", "Start no debug"], NoDebugStartCommandCode, new KeyGesture(Key.F5, KeyModifiers.Control));
		}

		public override void RegisterServices(IServiceCollection services)
			=> BuildServiceCollectionExtensions.AddBuild(services);				
	}
}

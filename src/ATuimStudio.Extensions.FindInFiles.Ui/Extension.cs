using ATuimStudio.Extensibility;
using ATuimStudio.Extensions.Core.Ui;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio.Extensions.FindInFiles
{
	public sealed class Extension : ATuimStudio.Extensibility.UiExtension
	{
		const string FindInFilesCommandCode = "FindInFiles";
		const string ReplaceInFilesCommandCode = "ReplaceInFiles";
		internal const string IdFindInFilesResult = "FindInFilesResult";
		internal readonly static Guid TypeFindInFilesResult = new Guid(0x7498c2f8, 0x266f, 0x4146, 0xa3, 0x22, 0x9e, 0x15, 0x63, 0xbd, 0xcd, 0xb8);

		public override void RegisterCommand(ICommandRegistrator commandRegistrator)
		{
			IDialogService dialogService = commandRegistrator.ServiceProvider.GetRequiredService<IDialogService>();

			commandRegistrator.Register(FindInFilesCommandCode, new RelayCommand(() =>
					dialogService.OpenNonModal<FindInFilesViewModel>(new DialogWindowParameters("Find in Files"), [""])
				),
				null
			);
			commandRegistrator.Register(ReplaceInFilesCommandCode, new AsyncRelayCommand(async cancellationToken =>
					dialogService.OpenNonModal<ReplaceInFilesViewModel>(new DialogWindowParameters("Replace in Files"), [""])
				),
				null
			);
		}

		public override void RegisterMenu(IMenuRegistrator menuRegistrator)
		{
			menuRegistrator.Register([("Edit", 400), ("Find in Files", 10)], FindInFilesCommandCode, new KeyGesture(Key.F, KeyModifiers.Control | KeyModifiers.Shift));
			menuRegistrator.Register([("Edit", 400), ("Replace in Files", 20)], ReplaceInFilesCommandCode, new KeyGesture(Key.H, KeyModifiers.Control | KeyModifiers.Shift));
		}

		public override void RegisterLayoutWindow(ILayoutWindowRegistrator layoutWindowRegistrator)
		{
			layoutWindowRegistrator.RegisterPaneFactory(TypeFindInFilesResult,
				static sp => ActivatorUtilities.CreateInstance<FindInFilesResultViewModel>(sp),
				static sp => new FindInFilesResultView());
		}

		public override void RegisterServices(IServiceCollection services)
			=> FindInFilesServiceCollectionExtensions.AddFindInFilesServices(services);
	}
}

using ATuimStudio.Extensibility;
using ATuimStudio.Extensions.Core.Ui;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio.Extensions.Terminal
{
	public sealed class Extension : ATuimStudio.Extensibility.UiExtension
	{
		const string CommandCode = "TerminalOpen";
		const string IdTerminal = "Terminal";
		readonly static Guid TypeTerminal = new Guid(0x903fa7b7, 0x350b, 0x463f, 0x82, 0x82, 0xae, 0xc9, 0x84, 0xaf, 0x7f, 0xce);
		static uint _instanceId = 0;

		public override void RegisterCommand(ICommandRegistrator commandRegistrator)
		{
			IUiWindowService uiWindowService = commandRegistrator.ServiceProvider.GetRequiredService<IUiWindowService>();
			commandRegistrator.Register(CommandCode, new RelayCommand(() =>
					uiWindowService.OpenPane(TypeTerminal, WellKnownLayoutConstants.IdBasicInfo, IdTerminal + Interlocked.Increment(ref _instanceId), "Terminal")
				),
				null
			);
		}

		public override void RegisterMenu(IMenuRegistrator menuRegistrator)
		{
			menuRegistrator.Register([("View", 300), ("Terminal", 20)], CommandCode, new KeyGesture(Key.OemTilde, KeyModifiers.Control));
		}

		public override void RegisterLayoutWindow(ILayoutWindowRegistrator layoutWindowRegistrator)
		{
			layoutWindowRegistrator.RegisterPaneFactory(TypeTerminal,
				static sp => ActivatorUtilities.CreateInstance<TerminalViewModel>(sp),
				static sp => new TerminalView());

			//layoutWindowRegistrator.RegisterParts(WellKnownLayoutConstants.LayoutBasic, static ctx =>
			//	ctx.Layout.FindPanesContainer(WellKnownLayoutConstants.IdBasicInfo)
			//		.AddPane(IdTerminal, "Terminal", TypeTerminal)
			//);
		}
	}
}

using ATuimStudio.Extensibility;
using ATuimStudio.Extensions.Core;
using ATuimStudio.Extensions.Core.Ui;
using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio.Extensions.OpenCode
{
	public sealed class Extension : ATuimStudio.Extensibility.UiExtension
	{
		const string IdOpenCodeChat = "OpenCodeChat";
		readonly static Guid TypeOpenCodeChatType = new Guid(0x56ac4ae5, 0x3978, 0x4eea, 0x84, 0xe3, 0x99, 0xd2, 0x77, 0x6b, 0xb, 0x0);

		public override void RegisterCommand(ICommandRegistrator commandRegistrator)
		{
			IUserOptionsRegistrator userOptionsRegistrator = commandRegistrator.ServiceProvider.GetRequiredService<IUserOptionsRegistrator>();
			userOptionsRegistrator.RegisterDefault(UserOptionsCodes.ServerPath, "opencode");
			userOptionsRegistrator.RegisterDefault(UserOptionsCodes.ServerPort, 4096);
			userOptionsRegistrator.RegisterDefault(UserOptionsCodes.PreferedProvider, "");
			userOptionsRegistrator.RegisterDefault(UserOptionsCodes.PreferedModel, "");
		}

		public override void RegisterLayoutWindow(ILayoutWindowRegistrator layoutWindowRegistrator)
		{
			layoutWindowRegistrator.RegisterPaneFactory(TypeOpenCodeChatType,
				static sp => ActivatorUtilities.CreateInstance<OpenCodeChatViewModel>(sp),
				static sp => new OpenCodeChatView());

			layoutWindowRegistrator.RegisterParts(WellKnownLayoutConstants.LayoutBasic, static ctx =>
				ctx.Layout.FindPanesContainer(WellKnownLayoutConstants.IdMainNavigation)
					.AddPane(IdOpenCodeChat, "OpenCode", TypeOpenCodeChatType)
			);
		}
	}
}

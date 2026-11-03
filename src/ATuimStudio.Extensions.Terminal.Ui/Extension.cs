using ATuimStudio.Extensibility;
using ATuimStudio.Extensions.Core.Ui;
using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio.Extensions.Terminal
{
	public sealed class Extension : ATuimStudio.Extensibility.UiExtension
	{
		const string IdTerminal = "Terminal";
		readonly static Guid TypeTerminal = new Guid(0x903fa7b7, 0x350b, 0x463f, 0x82, 0x82, 0xae, 0xc9, 0x84, 0xaf, 0x7f, 0xce);

		public override void RegisterLayoutWindow(ILayoutWindowRegistrator layoutWindowRegistrator)
		{
			layoutWindowRegistrator.RegisterPaneFactory(TypeTerminal,
				static sp => ActivatorUtilities.CreateInstance<TerminalViewModel>(sp),
				static sp => new TerminalView());

			layoutWindowRegistrator.RegisterParts(WellKnownLayoutConstants.LayoutBasic, static ctx =>
				ctx.Layout.FindPanesContainer(WellKnownLayoutConstants.IdBasicInfo)
					.AddPane(IdTerminal, "Terminal", TypeTerminal)
			);
		}
	}
}

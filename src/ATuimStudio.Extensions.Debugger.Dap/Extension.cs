using ATuimStudio.Extensibility;
using ATuimStudio.Extensions.Core;
using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio.Extensions.Debugger.Dap
{
	public sealed class Extension : ATuimStudio.Extensibility.UiExtension
	{
		public override void RegisterCommand(ICommandRegistrator commandRegistrator)
		{
			IUserOptionsRegistrator userOptionsRegistrator = commandRegistrator.ServiceProvider.GetRequiredService<IUserOptionsRegistrator>();
			userOptionsRegistrator.RegisterDefault(UserOptionsCodes.TerminalNoClose, true);
			userOptionsRegistrator.RegisterDefault(UserOptionsCodes.InterfaceType, "NamedPipe");
			userOptionsRegistrator.RegisterDefault(UserOptionsCodes.TcpHost, "127.0.0.1");
			userOptionsRegistrator.RegisterDefault(UserOptionsCodes.TcpPort, 8889);
		}

		public override void RegisterServices(IServiceCollection services)
			=> DapDebuggerServiceCollectionExtensions.AddDapDebugger(services);
	}
}

using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio.Extensions.Debugger.Mono
{
	public sealed class Extension : ATuimStudio.Extensibility.UiExtension
	{
		public override void RegisterServices(IServiceCollection services)
			=> MonoDebuggerServiceCollectionExtensions.AddMonoDebugger(services);
	}
}

using ATuimStudio.Extensions.Debug;
using ATuimStudio.Extensions.Debugger.Dap;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class DapDebuggerServiceCollectionExtensions
	{
		public static IServiceCollection AddDapDebugger(this IServiceCollection services)
			=> services
				.AddSingleton<IDebuggerRegistrator, DebuggerRegistrator>()
				;
	}
}

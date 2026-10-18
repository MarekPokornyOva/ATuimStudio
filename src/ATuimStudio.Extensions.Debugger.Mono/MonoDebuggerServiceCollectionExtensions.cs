using ATuimStudio.Extensions.Debug;
using ATuimStudio.Extensions.Debugger.Mono;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class MonoDebuggerServiceCollectionExtensions
	{
		public static IServiceCollection AddMonoDebugger(this IServiceCollection services)
			=> services
				.AddSingleton<IDebuggerRegistrator, DebuggerRegistrator>()
				;
	}
}

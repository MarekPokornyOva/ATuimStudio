using ATuimStudio.Extensions.Debug;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class DebugServiceCollectionExtensions
	{
		public static IServiceCollection AddDebug(this IServiceCollection services)
			=> services
				.AddSingleton<MonoDebuggerService>()
				.AddTransient<IDebuggerService>(sp => sp.GetRequiredService<MonoDebuggerService>())
				.AddTransient<IDebuggerProvider>(sp => sp.GetRequiredService<MonoDebuggerService>())
				.AddSingleton<IBreakpointManager, DefaultBreakpointManager>()
				.AddSingleton<IStackTraceProvider, DefaultStackTraceProvider>()
				;
	}
}

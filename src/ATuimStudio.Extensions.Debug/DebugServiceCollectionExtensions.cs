using ATuimStudio.Extensions.Debug;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class DebugServiceCollectionExtensions
	{
		public static IServiceCollection AddDebug(this IServiceCollection services)
			=> services
				.AddSingleton<DebuggerProvider>()
				.AddTransient<IDebuggerProvider>(ServiceProviderServiceExtensions.GetRequiredService<DebuggerProvider>)
				.AddTransient<IDebuggerFactory>(ServiceProviderServiceExtensions.GetRequiredService<DebuggerProvider>)
				.AddSingleton<IBreakpointManager, DefaultBreakpointManager>()
				.AddSingleton<IStackTraceProvider, DefaultStackTraceProvider>()
				;
	}
}

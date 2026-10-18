using ATuimStudio.Extensions.Debug;
using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio.Extensions.Debugger.Dap
{
	sealed class DebuggerRegistrator : IDebuggerRegistrator
	{
		public (string Key, Func<IServiceProvider, IDebuggerFactory> Factory) Register()
			=> ("Dap", static sp => ActivatorUtilities.CreateInstance<DebuggerFactory>(sp));
	}
}

using ATuimStudio.Extensions.Debug;

namespace ATuimStudio.Extensions.Debugger.Mono
{
	sealed class DebuggerRegistrator : IDebuggerRegistrator
	{
		public (string Key, Func<IServiceProvider, IDebuggerFactory> Factory) Register()
			=> ("Mono", static sp => new DebuggerFactory());
	}
}

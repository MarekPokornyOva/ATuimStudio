using ATuimStudio.Extensions.Debug;

namespace ATuimStudio.Extensions.Debugger.Mono
{
	sealed class DebuggerFactory : IDebuggerFactory
	{
		public Task<IDebugger> CreateDebuggerAsync(string path, string? arguments, IEnumerable<KeyValuePair<string, string>>? environmentVariables, CancellationToken cancellationToken)
			=> Task.FromResult<IDebugger>(new MonoDebugger(path, arguments, environmentVariables));
	}
}

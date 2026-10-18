namespace ATuimStudio.Extensions.Debug
{
	public interface IDebuggerFactory
	{
		Task<IDebugger> CreateDebuggerAsync(string path, string? arguments, IEnumerable<KeyValuePair<string, string>>? environmentVariables, CancellationToken cancellationToken);
	}
}

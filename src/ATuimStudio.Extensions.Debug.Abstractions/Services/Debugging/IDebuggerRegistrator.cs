namespace ATuimStudio.Extensions.Debug
{
	public interface IDebuggerRegistrator
	{
		(string Key, Func<IServiceProvider, IDebuggerFactory> Factory) Register();
	}
}

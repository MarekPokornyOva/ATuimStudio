namespace ATuimStudio.Extensions.Core
{
	public interface IApplicationLifetimeService
	{
		void RegisterDisposable(IDisposable disposable);
		void RegisterDisposable(IAsyncDisposable disposable);
	}

	public static class ApplicationLifetimeServiceExtensions
	{
		public static void RegisterDisposable(this IApplicationLifetimeService applicationLifetimeService, object obj)
		{
			if (obj is IDisposable disp)
				applicationLifetimeService.RegisterDisposable(disp);
			else if (obj is IAsyncDisposable adisp)
				applicationLifetimeService.RegisterDisposable(adisp);
		}
	}
}

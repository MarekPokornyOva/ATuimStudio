using ATuimStudio.Extensions.FindInFiles;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class FindInFilesServiceCollectionExtensions
	{
		public static IServiceCollection AddFindInFilesServices(this IServiceCollection services)
			=> services
					.AddSingleton<IFindInFiles, FindInFilesService>()
					;
	}
}

using ATuimStudio.Extensions.Build;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class BuildServiceCollectionExtensions
	{
		public static IServiceCollection AddBuild(this IServiceCollection services)
			=> services
					.AddSingleton<IBuildService, MsBuildService>()
					;
	}
}

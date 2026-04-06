using ATuimStudio.Extensions.Git;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class GitServiceCollectionExtensions
	{
		public static IServiceCollection AddGitServices(this IServiceCollection services)
			=> services
					.AddSingleton<ISourceRepositoryFactory, GitSourceRepositoryFactory>()
					;
	}
}

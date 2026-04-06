using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio.Extensions.Git
{
	sealed class GitSourceRepositoryFactory : ISourceRepositoryFactory
	{
		readonly IServiceProvider _serviceProvider;
		public GitSourceRepositoryFactory(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		public ISourceRepository Create(string path)
			=> ActivatorUtilities.CreateInstance<GitSourceRepository>(_serviceProvider, path);
	}
}

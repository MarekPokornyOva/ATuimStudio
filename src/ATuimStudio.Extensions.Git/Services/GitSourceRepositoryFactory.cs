using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace ATuimStudio.Extensions.Git
{
	sealed class GitSourceRepositoryFactory : ISourceRepositoryFactory, IDisposable
	{
		readonly ConcurrentDictionary<string, GitSourceRepository> _reposCache = new ConcurrentDictionary<string, GitSourceRepository>();

		readonly IServiceProvider _serviceProvider;
		public GitSourceRepositoryFactory(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		public ISourceRepository Create(string path)
			=> _reposCache.GetOrAdd(path, static (path, sp) => ActivatorUtilities.CreateInstance<GitSourceRepository>(sp, path), _serviceProvider);

		public void Dispose()
		{
			foreach (var item in _reposCache)
				item.Value.Dispose();
			_reposCache.Clear();
		}
	}
}

using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio.Extensions.Core
{
	public static class ServicesBuilderExtensions
	{
		public static IServiceCollection AddExtensionsCore(this IServiceCollection services)
		{
			PhysicalDiskSolutionDataStorage storage = new PhysicalDiskSolutionDataStorage();
			return services
				.AddSingleton<ISolutionDataStorage>(storage)
				.AddSingleton<ISolutionDataProvider>(storage)
				.AddSingleton<ISolutionService, DefaultSolutionService>()
				.AddSingleton<IOutputWriter, DefaultOutputWriter>()
				.AddSingleton<ITimeProvider, LocalTimeProvider>()
				.AddSingleton<IDiskWatchService, DefaultDiskWatchService>()
				.AddSingleton<IDocumentService, InternalDocumentService>()
				;
		}
	}
}

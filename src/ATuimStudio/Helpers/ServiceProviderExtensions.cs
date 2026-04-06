using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio
{
	static class ServiceProviderExtensions
	{
		internal static T CreateInstance<T>(this IServiceProvider sp, params object[] parameters)
			=> ActivatorUtilities.CreateInstance<T>(sp, parameters);
	}
}

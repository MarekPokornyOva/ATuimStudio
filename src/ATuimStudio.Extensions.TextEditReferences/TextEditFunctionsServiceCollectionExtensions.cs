using ATuimStudio.Extensions.TextEditReferences;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class TextEditReferencesServiceCollectionExtensions
	{
		public static IServiceCollection AddTextEditReferences(this IServiceCollection services)
		{
			return services
				.AddSingleton<RoslynReferencesFinder>()
				.AddSingleton<IReferencesFinder>(ServiceProviderServiceExtensions.GetRequiredService<RoslynReferencesFinder>)
				.AddSingleton<IQuickInfoProvider>(ServiceProviderServiceExtensions.GetRequiredService<RoslynReferencesFinder>)
				;
		}
	}
}

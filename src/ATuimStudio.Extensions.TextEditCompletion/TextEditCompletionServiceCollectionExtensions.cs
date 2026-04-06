using ATuimStudio.Extensions.TextEditCompletion;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class TextEditCompletionServiceCollectionExtensions
	{
		public static IServiceCollection AddTextEditCompletion(this IServiceCollection services)
		{
			return services
				.AddSingleton<ITextEditCompletionProvider, RecommenderCompletionProvider>()
				;
		}
	}
}

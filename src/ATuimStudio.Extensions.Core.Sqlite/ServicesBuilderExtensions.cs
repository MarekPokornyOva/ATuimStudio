using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio.Extensions.Core
{
	public static class SqliteServicesBuilderExtensions
	{
		public static IServiceCollection AddSqlLiteRepository(this IServiceCollection services)
		{
			return services
				.AddSingleton<IUserOptionsRepository>(static sp => new SqliteUserOptionsRepository(sp.GetRequiredService<IUserProfilePathProvider>().GetUserGlobalProfilePath()))
				.AddSingleton<IUserSolutionOptionsRepository>(static sp => new SqliteUserOptionsRepository(sp.GetRequiredService<IUserProfilePathProvider>().GetUserSolutionProfilePath()))
				;
		}
	}
}

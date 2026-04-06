using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio.Extensibility
{
	//It would be useful to use "static virtual members" but that's incompatible with .netstandard
	public class Extension
	{
		public virtual void RegisterServices(IServiceCollection services)
		{ }
	}
}

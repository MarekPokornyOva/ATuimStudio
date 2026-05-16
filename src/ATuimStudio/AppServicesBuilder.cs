using ATuimStudio.Extensibility;
using ATuimStudio.Extensions.Core;
using ATuimStudio.Extensions.Core.Ui;
using ATuimStudio.Services;
using ATuimStudio.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio
{
	static class AppServicesBuilder
	{
		internal static ServiceProvider BuildServiceProvider(ITopLevelVisualProvider topLevelVisualProvider)
		{
			IServiceCollection services = new ServiceCollection()
				.AddSingleton(typeof(IPub<>), typeof(Pub<>))
				.AddSingleton(typeof(ISub<>), typeof(Sub<>))
				.AddSingleton(typeof(ISubRegistrator<>), typeof(SubRegistrator<>))
				.AddSingleton<DockFactory>(static sp => ActivatorUtilities.CreateInstance<DockFactory>(sp))
				.AddTransient<IUiDocumentService>(static sp => sp.GetRequiredService<DockFactory>())
				.AddSingleton<IPluginPartsRegistrator, PluginPartsRegistrator>()
				.AddTransient<IMenuRegistrator>(ServiceProviderServiceExtensions.GetRequiredService<IPluginPartsRegistrator>)
				.AddTransient<ICommandRegistrator>(ServiceProviderServiceExtensions.GetRequiredService<IPluginPartsRegistrator>)
				.AddTransient<ILayoutWindowRegistrator>(ServiceProviderServiceExtensions.GetRequiredService<IPluginPartsRegistrator>)
				.AddTransient<IEditorDecoratorRegistrator>(ServiceProviderServiceExtensions.GetRequiredService<IPluginPartsRegistrator>)
				.AddSingleton<ILayoutManager>(ServiceProviderServiceExtensions.GetRequiredService<DockFactory>)
				.AddSingleton<ITopLevelVisualProvider>(topLevelVisualProvider)
				.AddSingleton<IDialogService, DefaultDialogService>()
				.AddSingleton<ICodeDiagnosticsManager, DefaultCodeDiagnosticsManages>()
				.AddExtensionsCore();
			PluginManager.Register(services);
			return services.BuildServiceProvider();
		}
	}
}

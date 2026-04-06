using ATuimStudio.Extensibility;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio
{
	static class PluginManager
	{
		readonly static UiExtension[] _extensions = [
			new ATuimStudio.Extensions.Git.Extension(),
			new ATuimStudio.Extensions.Build.Extension(),
			new ATuimStudio.Extensions.Debug.Extension(),
			new ATuimStudio.Extensions.TextEditCompletion.Extension()
			];

		internal static void Register(Action<IStyle> styleAppender)
		{
			foreach (UiExtension extension in _extensions)
				extension.RegisterStyles(styleAppender);
		}

		internal static void Register(IServiceCollection services)
		{
			foreach (UiExtension extension in _extensions)
				extension.RegisterServices(services);
		}

		internal static void Register(ServiceProvider sp)
		{
			ICommandRegistrator commandRegistrator = sp.GetRequiredService<ICommandRegistrator>();
			IMenuRegistrator menuRegistrator = sp.GetRequiredService<IMenuRegistrator>();
			ILayoutWindowRegistrator layoutWindowRegistrator = sp.GetRequiredService<ILayoutWindowRegistrator>();
			IEditorDecoratorRegistrator editorDecoratorRegistrator = sp.GetRequiredService<IEditorDecoratorRegistrator>();
			foreach (UiExtension extension in _extensions)
			{
				extension.RegisterCommand(commandRegistrator);
				extension.RegisterMenu(menuRegistrator);
				extension.RegisterLayoutWindow(layoutWindowRegistrator);
				extension.RegisterEditorDecorator(editorDecoratorRegistrator);
			}
			Array.Clear(_extensions);
		}
	}
}

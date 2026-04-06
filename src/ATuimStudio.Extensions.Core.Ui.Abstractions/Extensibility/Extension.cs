using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio.Extensibility
{
	//It would be useful to use "static virtual members" but that's incompatible with .netstandard
	public class UiExtension
	{
		public virtual void RegisterServices(IServiceCollection services)
		{ }
		public virtual void RegisterMenu(IMenuRegistrator menuRegistrator)
		{ }
		public virtual void RegisterCommand(ICommandRegistrator commandRegistrator)
		{ }
		public virtual void RegisterLayoutWindow(ILayoutWindowRegistrator layoutWindowRegistrator)
		{ }
		public virtual void RegisterEditorDecorator(IEditorDecoratorRegistrator editorDecoratorRegistrator)
		{ }
		public virtual void RegisterStyles(Action<IStyle> styleAppender)
		{ }
	}
}

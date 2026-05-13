using ATuimStudio.Extensibility;

namespace ATuimStudio.Services
{
	public interface IPluginPartsRegistrator : IMenuRegistrator, ICommandRegistrator, ILayoutWindowRegistrator, IEditorDecoratorRegistrator
	{
		IEnumerable<PluginPartsRegistrator.MenuRegistration> GetMenus();
		IReadOnlyDictionary<string, PluginPartsRegistrator.CommandRegistration> GetCommands();
		IReadOnlyCollection<PluginPartsRegistrator.LayoutPaneFactoryRegistration> GetLayoutPaneFactories();
		IReadOnlyCollection<(string LayoutName, Action<ILayoutWindowRegistratorContext> Registrator)> GetLayoutPartRegistrations();
		IReadOnlyCollection<PluginPartsRegistrator.EditorDecoratorRegistration> GetEditorDecorators();
		void Clear();
	}
}

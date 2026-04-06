using ATuimStudio.Extensibility;

namespace ATuimStudio.Services
{
	public interface IPluginPartsRegistrator : IMenuRegistrator, ICommandRegistrator, ILayoutWindowRegistrator, IEditorDecoratorRegistrator
	{
		IEnumerable<PluginPartsRegistrator.MenuRegistration> GetMenus();
		IReadOnlyDictionary<string, PluginPartsRegistrator.CommandRegistration> GetCommands();
		IReadOnlyCollection<PluginPartsRegistrator.LayoutRegistration> GetLayoutWindows();
		IReadOnlyCollection<PluginPartsRegistrator.EditorDecoratorRegistration> GetEditorDecorators();
		void Clear();
	}
}

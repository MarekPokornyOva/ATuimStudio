using ATuimStudio.Extensibility;
using Avalonia.Controls;
using Avalonia.Input;
using System.Windows.Input;

namespace ATuimStudio.Services
{
	public sealed class PluginPartsRegistrator : IPluginPartsRegistrator
	{
		readonly IServiceProvider _serviceProvider;
		public PluginPartsRegistrator(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		#region IMenuRegistrator
		readonly List<MenuRegistration> _menuRegistrations = [];

		public IServiceProvider ServiceProvider => _serviceProvider;

		public void Register(IEnumerable<string> segments, string commandCode, KeyGesture? gesture)
		{
			_menuRegistrations.Add(new MenuRegistration(segments, commandCode, gesture));
		}
		#endregion IMenuRegistrator

		#region ICommandRegistrator
		readonly Dictionary<string, CommandRegistration> _commandRegistrations = [];

		public void Register(string commandCode, ICommand command, Func<Stream>? imageDataProvider)
		{
			_commandRegistrations.Add(commandCode, new CommandRegistration(command, imageDataProvider));
		}
		#endregion ICommandRegistrator

		#region ILayoutWindowRegistrator
		readonly List<LayoutPaneFactoryRegistration> _layoutPaneFactories = [];
		readonly List<(string LayoutName, Action<ILayoutWindowRegistratorContext> Registrator)> _layoutPartRegistrations = [];

		public void RegisterPaneFactory(Guid type, Func<IServiceProvider, object> viewPanelFactory, Func<IServiceProvider, Control> viewFactory)
		{
			_layoutPaneFactories.Add(new LayoutPaneFactoryRegistration(type, viewPanelFactory, viewFactory));
		}

		public void RegisterParts(string layoutName, Action<ILayoutWindowRegistratorContext> registrator)
		{
			_layoutPartRegistrations.Add((layoutName, registrator));
		}
		#endregion ILayoutWindowRegistrator

		#region IEditorDecoratorRegistrator
		readonly List<EditorDecoratorRegistration> _editorLayoutRegistrations = [];
		public void Register(Action<IEditorDecoratorRegistratorContext> callback)
		{
			_editorLayoutRegistrations.Add(new EditorDecoratorRegistration(callback));
		}
		#endregion IEditorDecoratorRegistrator

		#region IPluginPartsRegistrator
		public record struct MenuRegistration(IEnumerable<string> Segments, string CommandCode, KeyGesture? Gesture);
		public record struct CommandRegistration(ICommand Command, Func<Stream>? ImageDataProvider);
		public record struct LayoutPaneFactoryRegistration(Guid Type, Func<IServiceProvider, object> ViewPanelFactory, Func<IServiceProvider, Control> ViewFactory);
		public record struct EditorDecoratorRegistration(Action<IEditorDecoratorRegistratorContext> Callback);

		IEnumerable<MenuRegistration> IPluginPartsRegistrator.GetMenus()
			=> _menuRegistrations;
		IReadOnlyDictionary<string, CommandRegistration> IPluginPartsRegistrator.GetCommands()
			=> _commandRegistrations;
		IReadOnlyCollection<LayoutPaneFactoryRegistration> IPluginPartsRegistrator.GetLayoutPaneFactories()
			=> _layoutPaneFactories;
		IReadOnlyCollection<(string LayoutName, Action<ILayoutWindowRegistratorContext> Registrator)> IPluginPartsRegistrator.GetLayoutPartRegistrations()
			=> _layoutPartRegistrations;
		IReadOnlyCollection<EditorDecoratorRegistration> IPluginPartsRegistrator.GetEditorDecorators()
			=> _editorLayoutRegistrations;
		void IPluginPartsRegistrator.Clear()
		{
			_menuRegistrations.Clear();
			_commandRegistrations.Clear();
			_layoutPaneFactories.Clear();
			_layoutPartRegistrations.Clear();
		}
		#endregion IPluginPartsRegistrator
	}
}

using ATuimStudio.Extensions.Core.Ui;
using Avalonia.Controls;

namespace ATuimStudio.Extensibility
{
	public interface ILayoutWindowRegistrator
	{
		void RegisterPaneFactory(Guid type, Func<IServiceProvider, object> viewPanelFactory, Func<IServiceProvider, Control> viewFactory);
		void RegisterParts(string layoutName, Action<ILayoutWindowRegistratorContext> registrator);
	}

	public interface ILayoutWindowRegistratorContext
	{
		ILayout Layout { get; }
	}
}

using ATuimStudio.Extensions.Core.Ui;
using ATuimStudio.ViewModels;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;

namespace ATuimStudio.Extensions.Core
{
	sealed class DefaultUiLayout : IUiLayout
	{
		readonly IRootDock _layout;
		readonly DockFactory _dockFactory;

		public DefaultUiLayout(IRootDock layout, DockFactory dockFactory)
		{
			_layout = layout;
			_dockFactory = dockFactory;
		}

		public void AddDocument(string id, Func<IUiLayoutAddDocumentContext, Document> factory)
		{
			_dockFactory.DocumentDock.AddDocument(id, sp => factory(new UiLayoutAddDocumentContext(_dockFactory)));
		}

		sealed class UiLayoutAddDocumentContext : IUiLayoutAddDocumentContext
		{
			readonly DockFactory _dockFactory;
			internal UiLayoutAddDocumentContext(DockFactory dockFactory)
			{
				_dockFactory = dockFactory;
			}

			public T CreateViewModel<T>(string id, string title) where T : Document
				=> _dockFactory.CreateViewModel<T>(id, title);

			public Document? FindById(string id)
				=> _dockFactory.FindById(id) as Document;
		}
	}
}

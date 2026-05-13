using ATuimStudio.Extensions.Core.Ui;
using ATuimStudio.ViewModels;
using Avalonia.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;
using Dock.Model.Mvvm.Core;
using System.Collections.ObjectModel;

namespace ATuimStudio
{
	class DockLayoutMaterializer
	{
		readonly IServiceProvider _serviceProvider;
		readonly Dictionary<Guid, (Func<IServiceProvider, object> ViewModelFactory, Func<IServiceProvider, Control> ViewFactory)> _paneTypeFactories;
		readonly Func<IDockable[], IList<IDockable>> CreateList;

		internal DockLayoutMaterializer(IServiceProvider serviceProvider, Dictionary<Guid, (Func<IServiceProvider, object> ViewModelFactory, Func<IServiceProvider, Control> ViewFactory)> paneTypeFactories, Func<IDockable[], IList<IDockable>> createList)
		{
			_serviceProvider = serviceProvider;
			_paneTypeFactories = paneTypeFactories;
			CreateList = createList;
		}

		internal IReadOnlyCollection<IDockable> MaterializeLayout(ILayout layout)
			=> new MappingReadOnlyCollection<ILayoutPart, IDockable>(layout.Parts, CreateDock);

		DockableBase CreateDock(ILayoutPart part)
		{
			DockableBase result =
				part is ILayoutPane layoutPane ? CreatePane(layoutPane) :
				part is ILayoutPanesContainer ? new ToolDock { Id = part.Id } :
				part is ILayoutDocumentsContainer docsContainer ? CreateDocuments(docsContainer) :
				new ProportionalDock { Id = part.Id };
				 
			SetProps(part, result);
			return result;
		}

		DockableBase CreatePane(ILayoutPane layoutPane)
		{
			if (!_paneTypeFactories.TryGetValue(layoutPane.Type, out var facts))
				return new ToolDock { Id = layoutPane.Id };
			return new GeneralTool(layoutPane.Id, facts.ViewModelFactory, facts.ViewFactory) { Id = layoutPane.Id, Title = layoutPane.Title };
		}

		CustomDocumentDock? _customDocumentDockSingleton;
		CustomDocumentDock CreateDocuments(ILayoutDocumentsContainer docsContainer)
		{
			CustomDocumentDock? customDocumentDock = _customDocumentDockSingleton;
			if (customDocumentDock != null)
				return customDocumentDock;

			//CustomDocumentDock must be reused from previous layout. Or document panes at least.
			customDocumentDock = _serviceProvider.CreateInstance<CustomDocumentDock>();
			customDocumentDock.Id = docsContainer.Id;
			customDocumentDock.IsCollapsable = false;
			_customDocumentDockSingleton = customDocumentDock;
			return customDocumentDock;
		}

		void SetProps(ILayoutPart props, DockableBase dockable)
		{
			Type type = dockable.GetType();
			foreach (KeyValuePair<string, object?> prop in props)
			{
				object? value = prop.Value;
				string name = prop.Key;
				type.GetProperty(name)!.SetValue(dockable, value);
			}

			if (props is ILayoutContainer cont)
			{
				int childrenCount = cont.Parts.Count;
				if (childrenCount != 0)
				{
					bool addSplitters = props is not ILayoutPanesContainer;
					IDockable[] childDockables = new IDockable[addSplitters ? childrenCount * 2 - 1 : childrenCount];
					int index = 0;
					foreach (ILayoutPart child in cont.Parts)
					{
						if (index != 0 && addSplitters)
							childDockables[index++] = new ProportionalDockSplitter { ResizePreview = true };
						childDockables[index++] = CreateDock(child);
					}
					type.GetProperty(nameof(DockBase.VisibleDockables))!.SetValue(dockable, CreateList(childDockables));
				}
			}
		}
	}
}

using ATuimStudio.Extensibility;
using ATuimStudio.Extensions.Core;
using ATuimStudio.Extensions.Core.Ui;
using ATuimStudio.Services;
using ATuimStudio.Views;
using Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio.ViewModels;

public class DockFactory : Factory, IUiDocumentService, ILayoutManager
{
	#region ctor
	readonly IServiceProvider _serviceProvider;
	readonly ISolutionService _solutionService;
	readonly DockLayoutMaterializer _dockLayoutMaterializer;

	public DockFactory(IServiceProvider serviceProvider, ISolutionService solutionService)
	{
		_serviceProvider = serviceProvider;
		_solutionService = solutionService;
		ActiveDockableChanged += DockFactory_ActiveDockableChanged;
		_dockLayoutMaterializer = new DockLayoutMaterializer(_serviceProvider, _paneTypeFactories, CreateList);

		//Initialize LayoutManager
		RegisterPaneFactory(WellKnownLayoutConstants.TypeSolutionExplorer, static sp => ActivatorUtilities.CreateInstance<SolutionViewModel>(sp), static sp => new SolutionView());
		RegisterPaneFactory(WellKnownLayoutConstants.TypeOutput, static sp => ActivatorUtilities.CreateInstance<OutputViewModel>(sp), static sp => new OutputView());

		Layout layout = new Layout();
		_currentLayout = layout;
		_layouts.Add(WellKnownLayoutConstants.LayoutBasic, layout);
		InitializeLayout(layout);
		layout.FindPanesContainer(WellKnownLayoutConstants.IdBasicInfo)
			.AddPane(WellKnownLayoutConstants.IdOutput, "Output", WellKnownLayoutConstants.TypeOutput);
	}
	#endregion ctor

	#region DockFactory
	internal CustomDocumentDock DocumentDock { get; private set; } = default!;
	internal IRootDock RootDock { get; private set; } = default!;

	internal void InitializeLayouts()
	{
		ILayoutManager layoutManager = _serviceProvider.GetRequiredService<ILayoutManager>();

		IPluginPartsRegistrator pluginPartsRegistrator = _serviceProvider.GetRequiredService<IPluginPartsRegistrator>();
		foreach (var pf in pluginPartsRegistrator.GetLayoutPaneFactories())
			layoutManager.RegisterPaneFactory(pf.Type, pf.ViewPanelFactory, pf.ViewFactory);

		//apply plugins windows
		IReadOnlyCollection<(string LayoutName, Action<ILayoutWindowRegistratorContext> Registrator)> layoutPartRegistrations = pluginPartsRegistrator.GetLayoutPartRegistrations();
		LayoutWindowRegistratorContext ctx = new LayoutWindowRegistratorContext();
		foreach ((string layoutName, Action<ILayoutWindowRegistratorContext> registrator) in layoutPartRegistrations)
		{
			if (!_layouts.TryGetValue(layoutName, out Layout? layout))
			{
				_layouts.Add(layoutName, layout = new Layout());
				InitializeLayout(layout);
			}
			ctx.Layout = layout;
			registrator(ctx);
		}

		SwitchLayout(WellKnownLayoutConstants.LayoutBasic);
	}

	public override IRootDock CreateLayout()
	{
		IReadOnlyCollection<IDockable> materialized = _dockLayoutMaterializer.MaterializeLayout(_currentLayout);
		IDockable mainLayout = materialized.First();

		IRootDock rootDock = base.CreateLayout();
		RootDock = rootDock;

		rootDock.IsCollapsable = false;
		rootDock.ActiveDockable = mainLayout;
		rootDock.DefaultDockable = mainLayout;
		rootDock.VisibleDockables = CreateList<IDockable>(mainLayout);

		rootDock.PinnedDock = null;
		DocumentDock = (CustomDocumentDock)(this.Find(rootDock, static x => x is CustomDocumentDock).Distinct().SingleOrDefault()
			?? throw new InvalidOperationException("No documents part found or many exists."));

		return rootDock;
	}

	//public override void InitLayout(IDockable layout)
	//{
	//	ContextLocator = new Dictionary<string, Func<object?>>
	//	{
	//		["SolutionExplorer"] = () => new object(), // a model
	//		["GitExplorer"] = () => new object(),
	//	};
	//
	//	base.InitLayout(layout);
	//}

	Layout _currentLayout;
	internal event EventHandler? LayoutRecreateRequested;

	sealed class LayoutWindowRegistratorContext : ILayoutWindowRegistratorContext
	{
		public ILayout Layout { get; internal set; } = default!;
	}
	#endregion DockFactory

	#region IUiDocumentService
	internal DocumentViewModel? ActiveDocument { get; private set; }
	void DockFactory_ActiveDockableChanged(object? sender, Dock.Model.Core.Events.ActiveDockableChangedEventArgs e)
	{
		if (e.Dockable is GeneralDocument gd && gd.ViewModel is DocumentViewModel dv)
			ActiveDocument = dv;
	}

	public IProjectInfo? GetActiveDocumentProject()
	{
		string? path = ActiveDocument?.FileData?.Path;
		if (path == null)
			return null;

		var solution = _solutionService.CurrentSolution;
		if (solution == null)
			return null;

		Dictionary<string, string> projects = solution.Projects.ToDictionary(static x => x.Path, static x => x.Name, StringComparer.Ordinal);
		while (true)
		{
			path = Path.GetDirectoryName(path)!;
			if (path == "")
				break;
			if (projects.TryGetValue(path, out string? projectName))
				return new UiProjectInfo(projectName);
		}

		return null;
	}

	public Task SaveCurrentDocument(CancellationToken cancellationToken)
		=> SaveDocument(this.ActiveDocument?.FileContent, cancellationToken);

	public async Task SaveAllOpenedDocuments(CancellationToken cancellationToken)
	{
		foreach (DocumentViewModel document in this.Documents)
			await SaveDocument(document.FileContent, cancellationToken);			
	}

	Task SaveDocument(AvaloniaEdit.Document.IDocument? document, CancellationToken cancellationToken)
		=> document == null
			? Task.CompletedTask
			: _solutionService.SaveDocumentAsync(document.FileName, document.Text, cancellationToken);

	public void AddSpecialDocument(string id, string title, Func<IServiceProvider, object> viewModelFactory, Func<IServiceProvider, Control> viewFactory)
		=> DocumentDock.AddDocument(id, title, viewModelFactory, viewFactory);

	internal IEnumerable<DocumentViewModel> Documents
	{
		get
		{
			IList<IDockable>? docks = DocumentDock.VisibleDockables;
			if (docks == null)
				return [];
			return docks.OfType<DocumentViewModel>();
		}
	}

	sealed record UiProjectInfo(string Name) : IProjectInfo;
	#endregion IUiDocumentService

	#region ILayoutManager
	readonly Dictionary<string, Layout> _layouts = new Dictionary<string, Layout>();

	public void SwitchLayout(string name)
	{
		if (!_layouts.TryGetValue(name, out Layout? layout))
			throw new ArgumentOutOfRangeException(nameof(name), "Layout is not registered.");

		_currentLayout = layout;
		Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
			LayoutRecreateRequested?.Invoke(null, EventArgs.Empty)
		);
	}

	readonly Dictionary<Guid, (Func<IServiceProvider, object> ViewPanelFactory, Func<IServiceProvider, Control> ViewFactory)> _paneTypeFactories = new Dictionary<Guid, (Func<IServiceProvider, object> ViewPanelFactory, Func<IServiceProvider, Control> ViewFactory)>();
	public void RegisterPaneFactory(Guid type, Func<IServiceProvider, object> viewPanelFactory, Func<IServiceProvider, Control> viewFactory)
	{
		_paneTypeFactories.Add(type, (viewPanelFactory, viewFactory));
	}

	static string RandomId()
		=> Guid.NewGuid().ToString("N");

	public object? TryFindViewModel(string id)
	{
		IDockable? dockable = this.Find(x => x.Id.EqualsOrdinal(id)).FirstOrDefault();
		if (dockable == null)
			return null;

		if (dockable is GeneralTool genTool)
			return genTool.ViewModelCreated ? genTool.GetViewModel(_serviceProvider) : null;

		if (dockable is GeneralDocument genDoc)
			return genDoc.ViewModel;

		return null;
	}

	static void InitializeLayout(Layout layout)
	{
		ILayoutWindow mainWindow = layout.AddWindow("", new PartProperties { { "Orientation", (int)Orientation.Horizontal } });
		ILayoutWindow mainSub1Window = mainWindow.AddWindow("", new PartProperties { { "Orientation", (int)Orientation.Vertical }, { "Proportion", 0.25 } });
		ILayoutPanesContainer leftTools = mainSub1Window.AddPanesContainer(WellKnownLayoutConstants.IdMainNavigation, new PartProperties { { "Alignment", (int)Alignment.Left } });
		leftTools.AddPane(WellKnownLayoutConstants.IdSolutionExplorer, "Solution Explorer", WellKnownLayoutConstants.TypeSolutionExplorer);
		ILayoutWindow mainSub2Window = mainWindow.AddWindow("", new PartProperties { { "Orientation", (int)Orientation.Vertical } });
		mainSub2Window.AddDocuments(WellKnownLayoutConstants.IdOpenedDocuments);
		ILayoutPanesContainer bottomTools = mainSub2Window.AddPanesContainer(WellKnownLayoutConstants.IdBasicInfo, new PartProperties { { "Proportion", 0.25 } });
	}

	#region layout model implementation
	class Layout : ILayout
	{
		public Layout()
		{ }

		public Layout(IEnumerable<ILayoutPart> parts)
		{
			_parts.AddRange(parts);
		}

		readonly List<ILayoutPart> _parts = new List<ILayoutPart>();
		public IReadOnlyCollection<ILayoutPart> Parts => _parts;

		public ILayoutWindow AddWindow(string id, PartProperties properties)
			=> ContainerHelper.AddWindow(_parts, id, properties);

		public ILayoutPanesContainer AddPanesContainer(string id, PartProperties properties)
			=> ContainerHelper.AddPanesContaner(_parts, id, properties);

		public void AddDocuments(string id)
			=> ContainerHelper.AddDocuments(_parts, id);

		public ILayoutPart? TryFindPart(string id)
			=> ContainerHelper.TryFindPart(_parts, id);
	}

	class LayoutWindow : PartProperties, ILayoutWindow
	{
		internal LayoutWindow(string id, PartProperties properties)
		{
			Id = id;
			this.AddRange(properties);
		}

		public string Id { get; }

		readonly List<ILayoutPart> _parts = new List<ILayoutPart>();
		public IReadOnlyCollection<ILayoutPart> Parts => _parts;

		public ILayoutWindow AddWindow(string id, PartProperties properties)
			=> ContainerHelper.AddWindow(_parts, id, properties);

		public ILayoutPanesContainer AddPanesContainer(string id, PartProperties properties)
			=> ContainerHelper.AddPanesContaner(_parts, id, properties);

		public void AddDocuments(string id)
			=> ContainerHelper.AddDocuments(_parts, id);

		public ILayoutPart? TryFindPart(string id)
			=> ContainerHelper.TryFindPart(_parts, id);
	}

	class LayoutPanesContaner : PartProperties, ILayoutPanesContainer
	{
		internal LayoutPanesContaner(string id, PartProperties properties)
		{
			Id = id;
			this.AddRange(properties);
		}

		public string Id { get; }

		readonly List<ILayoutPart> _parts = new List<ILayoutPart>();
		public IReadOnlyCollection<ILayoutPart> Parts => _parts;

		public ILayoutPanesContainer AddPane(string id, string title, Guid type)
		{
			ContainerHelper.AddPart<ILayoutPane, (string Title, Guid Type)>(_parts, id, (title, type), static (id, props) => new LayoutPane(id, props.Title, props.Type));
			return this;
		}

		public ILayoutPart? TryFindPart(string id)
			=> ContainerHelper.TryFindPart(_parts, id);
	}

	class LayoutDocumentsContainer : PartProperties, ILayoutDocumentsContainer
	{
		internal LayoutDocumentsContainer(string id)
		{
			Id = id;
		}

		public string Id { get; }

	}

	static class ContainerHelper
	{
		internal static TResult AddPart<TResult, TFactArg>(List<ILayoutPart> parts, string id, TFactArg factArg, Func<string, TFactArg, ILayoutPart> factory) where TResult : ILayoutPart
		{
			string key = id == "" ? RandomId() : id;
			ILayoutPart? part = parts.FirstOrDefault(x => x.Id.EqualsOrdinal(key));
			if (part == null)
				parts.Add(part = factory(key, factArg));
			if (part is TResult res)
				return res;
			throw new InvalidCastException("Another part already registered with the ID.");
		}

		internal static ILayoutWindow AddWindow(List<ILayoutPart> parts, string id, PartProperties properties)
			=> AddPart<ILayoutWindow, PartProperties>(parts, id, properties, static (id, props) => new LayoutWindow(id, props));

		internal static ILayoutPanesContainer AddPanesContaner(List<ILayoutPart> parts, string id, PartProperties properties)
			=> AddPart<ILayoutPanesContainer, PartProperties>(parts, id, properties, static (id, props) => new LayoutPanesContaner(id, props));

		internal static void AddDocuments(List<ILayoutPart> parts, string id)
			=> AddPart<ILayoutDocumentsContainer, PartProperties>(parts, id, null!, static (id, props) => new LayoutDocumentsContainer(id));

		internal static ILayoutPart? TryFindPart(List<ILayoutPart> parts, string id)
		{
			static IEnumerable<ILayoutPart> Nested(ILayoutPart part)
				=> (part is ILayoutContainer cont ? cont.Parts.SelectMany(Nested) : []).Prepend(part);
			return parts.SelectMany(Nested).FirstOrDefault(x => x.Id.EqualsOrdinal(id));
		}
	}

	class LayoutPane : PartProperties, ILayoutPane
	{
		internal LayoutPane(string id, string title, Guid type)
		{
			Id = id;
			Title = title;
			Type = type;
		}

		public string Id { get; }
		public string Title { get; }
		public Guid Type { get; }
	}
	#endregion layout model implementation
	#endregion ILayoutManager
}

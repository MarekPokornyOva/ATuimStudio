using ATuimStudio.Extensibility;
using ATuimStudio.Extensions.Core;
using ATuimStudio.Extensions.Core.Ui;
using ATuimStudio.Services;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using Dock.Model.Mvvm.Core;
using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio.ViewModels;

public class DockFactory : Factory, IUiDocumentService
{
	readonly IServiceProvider _serviceProvider;
	readonly ISolutionService _solutionService;
	public DockFactory(IServiceProvider serviceProvider, ISolutionService solutionService)
	{
		_serviceProvider = serviceProvider;
		_solutionService = solutionService;
		ActiveDockableChanged += DockFactory_ActiveDockableChanged;
	}

	internal CustomDocumentDock DocumentDock { get; private set; } = default!;
	internal IRootDock RootDock { get; private set; } = default!;

	public override IRootDock CreateLayout()
	{
		SolutionViewModel solutionExplorer = CreateViewModel<SolutionViewModel>("SolutionExplorer", "SolutionExplorer");
		OutputViewModel outputVM = CreateViewModel<OutputViewModel>("Output", "Output");

		var leftDock = new ProportionalDock
		{
			Proportion = 0.25,
			Orientation = Orientation.Vertical,
			ActiveDockable = null,
			VisibleDockables = CreateList<IDockable>
			(
				new ToolDock
				{
					Id = UiLayoutId.Left,
					ActiveDockable = solutionExplorer,
					VisibleDockables = CreateList<IDockable>(solutionExplorer),
					Alignment = Alignment.Left,
				}
			),
		};

		CustomDocumentDock documentDock = _serviceProvider.CreateInstance<CustomDocumentDock>();
		documentDock.Id = UiLayoutId.Documents;
		documentDock.IsCollapsable = false;
		DocumentDock = documentDock;

		//DebugInfoViewModel debugWindow = CreateViewModel<DebugInfoViewModel>("DebugWindow", "Debug view")

		var mainLayout = new ProportionalDock
		{
			Id = UiLayoutId.Main,
			Orientation = Orientation.Horizontal,
			VisibleDockables = CreateList<IDockable>
			(
				leftDock,
				new ProportionalDockSplitter { ResizePreview = true },
				new ProportionalDock
				{
					Orientation = Orientation.Vertical,
					VisibleDockables = CreateList<IDockable>
					(
						documentDock,
						new ProportionalDockSplitter { ResizePreview = true },
						new ToolDock
						{
							Id = UiLayoutId.BelowDocuments,
							Proportion = 0.25,
							VisibleDockables = CreateList<IDockable>(outputVM),
						}
					)
				}
			)
		};

		IRootDock rootDock = base.CreateLayout();
		RootDock = rootDock;

		rootDock.IsCollapsable = false;
		rootDock.ActiveDockable = mainLayout;
		rootDock.DefaultDockable = mainLayout;
		rootDock.VisibleDockables = CreateList<IDockable>(mainLayout);

		rootDock.PinnedDock = null;

		//apply plugins' windows
		IReadOnlyCollection<PluginPartsRegistrator.LayoutRegistration> pluginWindows = _serviceProvider.GetRequiredService<IPluginPartsRegistrator>().GetLayoutWindows();
		if (pluginWindows.Count != 0)
		{
			LayoutWindowRegistratorContext layoutWindowRegistratorContext = new LayoutWindowRegistratorContext(this);
			foreach (PluginPartsRegistrator.LayoutRegistration window in pluginWindows)
			{
				string id = window.ParentIds;
				IDockable? parent = FindById(window.ParentIds);
				if (parent is not DockBase db)
					continue;
				if (window.Factory(layoutWindowRegistratorContext) is IDockable d)
					(db.VisibleDockables ??= CreateList<IDockable>()).Add(d);
			}
		}

		return rootDock;
	}

	internal T CreateViewModel<T>(string id, string title) where T : IDockable
	{
		T result = _serviceProvider.CreateInstance<T>();
		result.Id = id;
		result.Title = title;
		return result;
	}
	internal IDockable? FindById(string id)
		=> this.FindDockable(RootDock, x => x.Id.EqualsOrdinal(id));

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

	internal DocumentViewModel? ActiveDocument { get; private set; }
	void DockFactory_ActiveDockableChanged(object? sender, Dock.Model.Core.Events.ActiveDockableChangedEventArgs e)
	{
		if (e.Dockable is DocumentViewModel dvm)
			ActiveDocument = dvm;
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

	sealed class LayoutWindowRegistratorContext : ILayoutWindowRegistratorContext
	{
		readonly DockFactory _dockFactory;
		public LayoutWindowRegistratorContext(DockFactory dockFactory)
		{
			_dockFactory = dockFactory;
		}

		T ILayoutWindowRegistratorContext.CreateViewModel<T>(string id, string title)
			=> _dockFactory.CreateViewModel<T>(id, title);
	}

	sealed record UiProjectInfo(string Name) : IProjectInfo;
}

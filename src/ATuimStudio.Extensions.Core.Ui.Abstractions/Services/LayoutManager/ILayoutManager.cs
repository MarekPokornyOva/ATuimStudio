using Avalonia.Controls;

namespace ATuimStudio.Extensions.Core.Ui
{
	public interface ILayoutManager
	{
		void SwitchLayout(string name);
		void RegisterPaneFactory(Guid type, Func<IServiceProvider, object> viewPanelFactory, Func<IServiceProvider, Control> viewFactory);
		object? TryFindViewModel(string id);
	}

	public static class WellKnownLayoutConstants
	{
		public const string LayoutBasic = "Basic";
		public const string IdMainNavigation = "MainNavigation";
		public const string IdBasicInfo = "BasicInfo";
		public const string IdOpenedDocuments = "OpenedDocuments";
		public const string IdSolutionExplorer = "SolutionExplorer";
		public const string IdOutput = "Output";
		public readonly static Guid TypeSolutionExplorer = new Guid(0x2af1df58, 0x2341, 0x4a89, 0xb9, 0xa5, 0x67, 0xff, 0x3e, 0x72, 0xd1, 0x2a);
		public readonly static Guid TypeOutput = new Guid(0x2eea10dc, 0x501f, 0x48ae, 0xb9, 0xae, 0x76, 0x4f, 0x57, 0x7b, 0x31, 0x15);
	}

	public class PartProperties : Dictionary<string, object?>
	{
	}

	public interface ILayoutPart : IReadOnlyDictionary<string, object?>
	{
		string Id { get; }
	}

	public interface ILayoutContainer
	{
		IReadOnlyCollection<ILayoutPart> Parts { get; }
		ILayoutPart? TryFindPart(string id);
	}

	public static class LayoutContainerExtensions
	{
		public static ILayoutPart FindPart(this ILayoutContainer container, string id)
			=> container.TryFindPart(id) ?? throw new InvalidOperationException(string.Concat("No layout part with ID '", id, "' found."));

		public static ILayoutContainerManager FindContainerManager(this ILayoutContainer container, string id)
			=> FindType<ILayoutContainerManager>(container, id);

		public static ILayoutPanesContainer FindPanesContainer(this ILayoutContainer container, string id)
			=> FindType<ILayoutPanesContainer>(container, id);

		static T FindType<T>(ILayoutContainer container, string id)
		{
			ILayoutPart part = FindPart(container, id);
			if (part is T res)
				return res;
			throw new InvalidOperationException("Expected part is not expected type.");
		}
	}

	public interface ILayoutContainerManager : ILayoutContainer
	{
		ILayoutWindow AddWindow(string id, PartProperties properties);
		ILayoutPanesContainer AddPanesContainer(string id, PartProperties properties);
		void AddDocuments(string id);
	}

	public interface ILayout : ILayoutContainerManager
	{
	}

	public interface ILayoutWindow : ILayoutPart, ILayoutContainerManager
	{
	}

	public interface ILayoutPanesContainer : ILayoutPart, ILayoutContainer
	{
		ILayoutPanesContainer AddPane(string id, string title, Guid type);
	}

	public interface ILayoutDocumentsContainer : ILayoutPart
	{ }

	public interface ILayoutPane : ILayoutPart
	{
		string Title { get; }
		Guid Type { get; }
	}
}

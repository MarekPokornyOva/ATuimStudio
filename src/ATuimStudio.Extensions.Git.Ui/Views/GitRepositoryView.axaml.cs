using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace ATuimStudio.Extensions.Git;

public partial class GitRepositoryView : UserControl
{
	public GitRepositoryView()
	{
		InitializeComponent();

		this.Loaded += GitRepositoryView_Loaded;
	}

	ScrollBar? _commitListContainerVerticalScrollbar;
	private void GitRepositoryView_Loaded(object? sender, RoutedEventArgs e)
	{
		this.Loaded -= GitRepositoryView_Loaded;

		foreach (Visual child in CommitListContainer.GetVisualDescendants())
			if (child is ScrollBar sb && child.Name.EqualsOrdinal("PART_VerticalScrollbar"))
			{
				sb.ValueChanged += CommitListContainerVerticalScrollHandler;
				_commitListContainerVerticalScrollbar = sb;
				break;
			}
	}

	void CommitListContainerVerticalScrollHandler(object? sender, RangeBaseValueChangedEventArgs e)
	{
		CommitGraphControlScroller.Offset = new Vector(CommitGraphControlScroller.Offset.X, e.NewValue);
	}

	~GitRepositoryView()
	{
		if (_commitListContainerVerticalScrollbar != null)
		{
			_commitListContainerVerticalScrollbar.ValueChanged -= CommitListContainerVerticalScrollHandler;
			_commitListContainerVerticalScrollbar = null;
		}
	}
}
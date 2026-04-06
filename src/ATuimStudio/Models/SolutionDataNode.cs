using ATuimStudio.Extensions.Core;
using System.Collections.ObjectModel;

namespace ATuimStudio.Models;

public sealed class SolutionDataNode
{
	public ObservableCollection<SolutionDataNode>? Children { get; }
	public string Title { get; }
	public bool IsExpanded { get; set; }
	public INamedData? Data { get; }
	public SolutionDataNode? Parent { get; }

	public SolutionDataNode(string title)
	{
		Title = title;
	}

	public SolutionDataNode(SolutionDataNode? parent, INamedData data, bool hasChildren)
	{
		Parent = parent;
		Data = data;
		Title = data.Name;
		Children = hasChildren ? new ObservableCollection<SolutionDataNode>() : null;
	}
}

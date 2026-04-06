using ATuimStudio.Models;
using ATuimStudio.Extensions.Core;
using Dock.Model.Mvvm.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATuimStudio.ViewModels;

public sealed partial class SolutionViewModel : Tool
{
	public ObservableCollection<SolutionDataNode> SolutionNodes { get; } = [];
	private SolutionDataNode? _selectedItem;
	public SolutionDataNode? SelectedItem { get => _selectedItem; set { _selectedItem = value; SelectedItemChanged(); } }

	[EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable CS8618
	public SolutionViewModel()
	{ }
#pragma warning restore CS8618

	readonly ISolutionService _solutionService;
	readonly IPub<IFileOpenEvent> _pubFileOpenEvent;
	readonly IDialogService _dialogService;
	public SolutionViewModel(ISolutionService solutionService, IPub<IFileOpenEvent> pubFileOpenEvent, IDialogService dialogService)
	{
		_solutionService = solutionService;
		_dialogService = dialogService;

		solutionService.OnSolutionLoaded += (sender, e) => SolutionChanged(e.Solution);
		solutionService.OnSolutionUnloaded += (sender, e) => SolutionChanged(null);
		_pubFileOpenEvent = pubFileOpenEvent;
		SolutionChanged(null);
	}

	static readonly SolutionDataNode _noSolutionNode = new SolutionDataNode("No solution loaded");
	void SolutionChanged(ISolutionData? solutionData)
	{
		SolutionNodes.Clear();
		if (solutionData == null)
		{
			_noSolutionNode.IsExpanded = true;
			SelectedItem = _noSolutionNode;
			SolutionNodes.Add(_noSolutionNode);
		}
		else
		{
			SolutionDataNode solutionNode = new SolutionDataNode(null, solutionData, true) { IsExpanded = true };
			foreach (IProjectData proj in solutionData.Projects)
			{
				SolutionDataNode projNode = new SolutionDataNode(solutionNode, proj, true);
				solutionNode.Children!.Add(projNode);
				FillNode(projNode, proj.Items);
			}
			SelectedItem = solutionNode;
			SolutionNodes.Add(solutionNode);
		}

		static void FillNode(SolutionDataNode parNode, IEnumerable<IProjectItemData> items)
		{
			foreach (IProjectItemData item in items)
			{
				SolutionDataNode node;
				if (item is IProjectDirectoryData dir)
				{
					node = new SolutionDataNode(parNode, item, true);
					FillNode(node, dir.Items);
				}
				else
					node = new SolutionDataNode(parNode, item, false);
				parNode.Children!.Add(node);
			}
		}
	}

	void SelectedItemChanged()
	{
		if (_selectedItem?.Data is IProjectFileData fileData)
			_pubFileOpenEvent.Raise(new FileOpenEvent(fileData));
	}

	[RelayCommand]
	void DeleteFile()
	{
		if (_selectedItem?.Data is IProjectFileData fileData)
		{
			_solutionService.DeleteFile(fileData.Path);
			_selectedItem.Parent!.Children!.Remove(_selectedItem);
		}
	}

	[RelayCommand]
	async Task NewFileAsync()
	{
		if (_selectedItem?.Data is IProjectData projData)
		{
			await NewFileIn(projData.Path, _selectedItem);
			return;
		}
		if (_selectedItem?.Data is IProjectFileData fileData)
			await NewFileIn(Path.GetDirectoryName(fileData.Path)!, _selectedItem.Parent!);
	}

	async Task NewFileIn(string path, SolutionDataNode parentNode)
	{
		string? filename = await _dialogService.OpenModal<NewProjectFileNameDialogViewModel, string?>(new DialogWindowParameters("New file"), [path]);
		if (filename == null)
			return;
		IProjectFileData? itemData = _solutionService.CreateFile(Path.Combine(path, filename));
		if (itemData == null)
			return;

		//asort new item into solution tree
		SolutionDataNode newChildNode = new SolutionDataNode(parentNode, itemData, false);
		int newIndex = 0;
		foreach (SolutionDataNode childNode in parentNode.Children!)
		{
			if (string.Compare(childNode.Title, filename, PathHelper.PathStringComparison) > 0)
			{
				parentNode.Children!.Insert(newIndex, newChildNode);
				return;
			}
			newIndex++;
		}
		parentNode.Children!.Add(newChildNode);
	}
}

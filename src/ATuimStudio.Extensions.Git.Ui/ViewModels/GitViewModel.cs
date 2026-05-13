using ATuimStudio.Common;
using ATuimStudio.Extensions.Core;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ATuimStudio.Extensions.Git;

public sealed partial class GitViewModel : ViewModelBase<GitViewModel.RepoNode>
{
	public ObservableCollection<StatusNode> StatusUnstaged { get; } = [];
	public ObservableCollection<StatusNode> StatusStaged { get; } = [];

	[ObservableProperty]
	string? _commitMessage;

	[ObservableProperty]
	bool _amend;

	readonly IDiskWatchService _diskWatchService;
	public GitViewModel(ISourceRepositoryFactory sourceRepositoryFactory, ISolutionService solutionService, IDiskWatchService diskWatchService) : base(sourceRepositoryFactory, solutionService, true)
	{
		_diskWatchService = diskWatchService;
		_diskWatchHandler = new DebouncingHandler(DiskContentChanged, 100).Handle;

		PostInitialize();
	}

	protected override void SelectedRepoChanged(RepoNode? value)
	{
		RefreshStatus();
	}

	readonly Action<FileSystemEventArgs> _diskWatchHandler;
	protected override RepoNode CreateRepoNode(string path, string title, ISourceRepository repository)
		=> new RepoNode(path, title, repository, path == null ? null : _diskWatchService.Watch(path, _diskWatchHandler, null));

	static bool FilterFileStatus(IFileStatus fileStatus)
		=> !fileStatus.Status.HasFlag(FileStatus.Ignored);

	void DiskContentChanged(FileSystemEventArgs args)
	{
		RepoNode? selectedRepo = SelectedRepo;
		if (selectedRepo == _allRepos || (selectedRepo != null && PathHelper.IsRootPathOf(selectedRepo.Path, args.FullPath)))
			Dispatcher.UIThread.Invoke(RefreshStatus);
	}

	#region build structures
	readonly static char[] _pathSeparators = ['/'];
	static (IEnumerable<StatusNode> unstaged, IEnumerable<StatusNode> staged) BuildStatusesStructure(IEnumerable<IFileStatus> statuses, ISourceRepository repository)
	{
		IFileStatus[] fileStatuses = [.. statuses.Where(FilterFileStatus)];

		static bool IsStaged(IFileStatus fileStatus)
		{
			FileStatus st = fileStatus.Status;
			return st == FileStatus.NewInIndex ||
					st == FileStatus.ModifiedInIndex ||
					st == FileStatus.DeletedFromIndex ||
					st == FileStatus.RenamedInIndex;
		}

		static IEnumerable<StatusNode> GetTree(IEnumerable<IFileStatus> fileStatuses, ISourceRepository repository)
		{
			IEnumerable<TreeMaker.INode<FileStatus>> tree = TreeMaker.Make(
				fileStatuses.Select(static item => (item.Path.Split(_pathSeparators, StringSplitOptions.RemoveEmptyEntries), item.Status))
				);

			static IEnumerable<StatusNode> ConvertNodes(IEnumerable<TreeMaker.INode<FileStatus>> nodes, string parentPath, ISourceRepository repository)
				=> nodes.OrderBy(static x => x.Name).Select(x => { string fullpath = Path.Combine(parentPath, x.Name); return new StatusNode(fullpath, x.Name, ConvertNodes(x.Children, fullpath, repository), repository, x.HasData ? x.Data : default); });
			return ConvertNodes(tree, "", repository);
		}
		return (GetTree(fileStatuses.Where(static x => !IsStaged(x)), repository), GetTree(fileStatuses.Where(IsStaged), repository));
	}
	#endregion build structures

	#region help classes
	public record StatusNode(string Path, string Name, IEnumerable<StatusNode> Children, ISourceRepository Repository, FileStatus? Status)
	{
		public bool IsExpanded { get; set; } = true;
	}
	public record RepoNode(string Path, string Title, ISourceRepository Repository, IWatchClient? WatchClient) : ViewModelBase.RepoNodeBase(Path, Title, Repository);
	#endregion help classes

	protected override void Dispose(bool disposing)
	{
		if (disposing)
			foreach (RepoNode repoNode in Repos)
				repoNode.WatchClient?.Dispose();

		base.Dispose(disposing);
	}

	void RefreshStatus()
	{
		RepoNode? selectedRepo = SelectedRepo;
		ISourceRepository? repository = selectedRepo?.Repository;

		StatusUnstaged.Clear();
		StatusStaged.Clear();

		if (selectedRepo == _allRepos)
		{
			foreach (RepoNode repo in Repos.Skip(1))
			{
				(IEnumerable<StatusNode> unstaged, IEnumerable<StatusNode> staged) = BuildStatusesStructure(repo.Repository.GetFilesStatus(), repo.Repository);
				StatusUnstaged.Add(new StatusNode("", repo.Path, unstaged, repo.Repository, null));
				StatusStaged.Add(new StatusNode("", repo.Path, staged, repo.Repository, null));
			}
		}
		else
			if (repository != null)
			{
				(IEnumerable<StatusNode> unstaged, IEnumerable<StatusNode> staged) = BuildStatusesStructure(repository.GetFilesStatus(), repository);
				StatusUnstaged.AddRange(unstaged);
				StatusStaged.AddRange(staged);
			}
	}

	[RelayCommand]
	void UndoChanges(StatusNode statusNode)
	{
		statusNode.Repository.Checkout(null, statusNode.Path);
		RefreshStatus();
	}

	[RelayCommand]
	void Stage(StatusNode statusNode)
	{
		statusNode.Repository.Stage(statusNode.Path);
		RefreshStatus();
	}

	[RelayCommand]
	void Unstage(StatusNode statusNode)
	{
		statusNode.Repository.Unstage(statusNode.Path);
		RefreshStatus();
	}

	[RelayCommand]
	void UnstageAll()
	{
		if (SelectedRepo == null || SelectedRepo == _allRepos)
			return;
		SelectedRepo.Repository.Unstage("*");
		RefreshStatus();
	}

	[RelayCommand]
	void CommitAll()
		=> Commit(true);

	[RelayCommand]
	void CommitStaged()
		=> Commit(false);

	void Commit(bool stageAll)
	{
		if (SelectedRepo == null)
			return;
		ISourceRepository repo = SelectedRepo.Repository;
		if (stageAll)
			repo.Stage("*");
		repo.Commit(CommitMessage ?? "", Amend);

		CommitMessage = "";
		RefreshStatus();
	}
}

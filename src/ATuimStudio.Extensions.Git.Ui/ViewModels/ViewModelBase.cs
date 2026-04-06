using ATuimStudio.Common;
using ATuimStudio.Extensions.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ATuimStudio.Extensions.Git
{
	public abstract class ViewModelBase
	{
		public record BranchNode(string Name, IEnumerable<BranchNode> Children, IBranch? Branch)
		{
			public bool IsExpanded { get; set; }
		}
		public record RepoNodeBase(string Path, string Title, ISourceRepository Repository);
	}

	public abstract partial class ViewModelBase<TRepoNode> : Document, IDisposable where TRepoNode: ViewModelBase.RepoNodeBase
	{
		public ObservableCollection<TRepoNode> Repos { get; } = [];

		[ObservableProperty]
		private TRepoNode? _selectedRepo;

		public ObservableCollection<ViewModelBase.BranchNode> Branches { get; } = [];

		[ObservableProperty]
		private ViewModelBase.BranchNode? _selectedBranch;

		[EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable CS8618
		public ViewModelBase()
		{ }
#pragma warning restore CS8618

		protected readonly TRepoNode _allRepos;

		protected readonly ISourceRepositoryFactory _sourceRepositoryFactory;
		readonly ISolutionService _solutionService;
		readonly bool _supportAllRepos;
		public ViewModelBase(ISourceRepositoryFactory sourceRepositoryFactory, ISolutionService solutionService, bool supportAllRepos) : this()
		{
			_sourceRepositoryFactory = sourceRepositoryFactory;
			_solutionService = solutionService;
			_supportAllRepos = supportAllRepos;

			_allRepos = CreateRepoNode(null!, "All", null!);

			solutionService.OnSolutionLoaded += SolutionService_OnSolutionLoaded;
			solutionService.OnSolutionUnloaded += SolutionService_OnSolutionUnloaded;
		}

		void Load(ISolutionData solutionData)
		{
			string[] gitDirectories = GetGitDirectories(solutionData);

			if (gitDirectories.Length == 0)
				return;

			if (_supportAllRepos && gitDirectories.Length != 1)
				Repos.Add(_allRepos);
			foreach (string repoPath in gitDirectories)
				Repos.Add(CreateRepoNode(repoPath, Path.GetFileName(repoPath), _sourceRepositoryFactory.Create(repoPath)));
			SelectedRepo = Repos[0];
		}

		string[] GetGitDirectories(ISolutionData solutionData)
		{
			ISolutionData? solution = _solutionService.CurrentSolution;
			if (solution == null)
				return [];
			string solutionPath = solution.Path;
			return Directory.Exists(Path.Combine(solutionPath, ".git"))
				? [solutionPath]
				: [];
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				_solutionService.OnSolutionLoaded -= SolutionService_OnSolutionLoaded;
				_solutionService.OnSolutionUnloaded -= SolutionService_OnSolutionUnloaded;
				foreach (TRepoNode repoNode in Repos)
					repoNode.Repository?.Dispose();
			}
		}

		protected abstract TRepoNode CreateRepoNode(string path, string title, ISourceRepository repository);

		partial void OnSelectedRepoChanged(TRepoNode? value)
		{
			ISourceRepository? repository = value?.Repository;

			Branches.Clear();
			if (value != _allRepos && repository != null)
			{
				string currentBranch = repository.GetCurrentBranch();
				Branches.AddRange(BuildBranchesStructure(repository.GetBranches(), currentBranch, this));
			}

			SelectedRepoChanged(value);
		}
		protected virtual void SelectedRepoChanged(TRepoNode? value)
		{ }

		partial void OnSelectedBranchChanged(ViewModelBase.BranchNode? value)
			=> SelectedBranchChanged(value);
		protected virtual void SelectedBranchChanged(ViewModelBase.BranchNode? value)
		{ }

		void SolutionService_OnSolutionLoaded(object? sender, SolutionLoadedEventArgs e)
		{
			Load(e.Solution);
		}

		void SolutionService_OnSolutionUnloaded(object? sender, SolutionUnloadedEventArgs e)
		{
			Repos.Clear();
			SelectedRepo = null;
			Branches.Clear();
			SelectedBranch = null;
		}

		#region build structures
		readonly static char[] _pathSeparators = ['/'];
		protected static IEnumerable<ViewModelBase.BranchNode> BuildBranchesStructure(IEnumerable<IBranch> branches, string currentBranch, ViewModelBase<TRepoNode> viewModel)
		{
			IEnumerable<TreeMaker.INode<IBranch>> tree = TreeMaker.Make(
				branches.Select(static branch => (branch.Name.Split(_pathSeparators, StringSplitOptions.RemoveEmptyEntries), branch))
				);

			static IEnumerable<ViewModelBase.BranchNode> ConvertNodes(IEnumerable<TreeMaker.INode<IBranch>> nodes, string currentBranch, ViewModelBase<TRepoNode> viewModel)
				=> nodes.OrderBy(static x => x.Name).Select(x =>
				{
					ViewModelBase.BranchNode branchNode = new ViewModelBase.BranchNode(x.Name, ConvertNodes(x.Children, currentBranch, viewModel), x.HasData ? x.Data : default);
					if (x.HasData && x.Data.Name == currentBranch)
						viewModel.SelectedBranch = branchNode;
					return branchNode;
				});
			return ConvertNodes(tree, currentBranch, viewModel);
		}
		#endregion build structures
	}
}

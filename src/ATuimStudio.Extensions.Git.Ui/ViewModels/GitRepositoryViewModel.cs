using ATuimStudio.Extensions.Core;
using System.Collections.ObjectModel;

namespace ATuimStudio.Extensions.Git
{
	public sealed class GitRepositoryViewModel : ViewModelBase<ViewModelBase.RepoNodeBase>
	{
		public ObservableCollection<ICommit> Commits { get; } = [];

		public GitRepositoryViewModel(ISourceRepositoryFactory sourceRepositoryFactory, ISolutionService solutionService) : base(sourceRepositoryFactory, solutionService, false)
		{
			PostInitialize();
		}

		protected override ViewModelBase.RepoNodeBase CreateRepoNode(string path, string title, ISourceRepository repository)
			=> new ViewModelBase.RepoNodeBase(path, title, repository);

		protected override void SelectedBranchChanged(ViewModelBase.BranchNode? value)
		{
			Commits.Clear();
			ISourceRepository? repository = SelectedRepo?.Repository;
			if (repository != null)
			{
				IBranch? branch = value?.Branch;
				if (branch != null)
					Commits.AddRange(repository.GetCommits(branch));
			}
		}

		internal void SelectBranch(string repo, string branch)
		{
			ViewModelBase.RepoNodeBase? repoToSelect = Repos.FirstOrDefault(x => x.Path.EqualsOrdinal(repo));
			if (repoToSelect == null)
				return;
			SelectedRepo = repoToSelect;

			ViewModelBase.BranchNode? branchToSelect = Branches.FirstOrDefault(x => x.Name.EqualsOrdinal(branch));
			if (branchToSelect == null)
				return;
			SelectedBranch = branchToSelect;
		}
	}
}

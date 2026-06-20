using ATuimStudio.Extensions.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace ATuimStudio.Extensions.Git
{
	public sealed partial class GitRepositoryViewModel : ViewModelBase<ViewModelBase.RepoNodeBase>
	{
		public int IncommingCount { get; private set; }
		public int OutgoingCount { get; private set; }
		[ObservableProperty]
		public IEnumerable<ICommit>? _commits;

		public GitRepositoryViewModel(ISourceRepositoryFactory sourceRepositoryFactory, ISolutionService solutionService, IUserOptionsManager userOptionsManager) : base(sourceRepositoryFactory, solutionService, userOptionsManager, false)
		{
			PostInitialize();
		}

		protected override ViewModelBase.RepoNodeBase CreateRepoNode(string path, string title, ISourceRepository repository)
			=> new ViewModelBase.RepoNodeBase(path, title, repository);

		protected override void SelectedBranchChanged(ViewModelBase.BranchNode? value)
		{
			ISourceRepository? repository = SelectedRepo?.Repository;
			if (repository != null)
			{
				IBranch? branch = value?.Branch;
				if (branch != null)
				{
					ICommit[] commits = [.. repository.GetCommits(branch)];
					List<ICommit>? incomming = null;
					OutgoingCount = 0;
					if (!branch.IsRemote)
					{
						IBranch? remote = branch.GetRemote();
						if (remote != null && remote.Tip != branch.Tip)
						{
							List<ICommit> inc = new List<ICommit>(16);
							foreach (ICommit commit in repository.GetCommits(remote))
							{
								int pos = Array.IndexOf(commits, commit);
								if (pos == -1)
									inc.Add(commit);
								else
								{
									OutgoingCount = pos;
									break;
								}
							}
							if (inc.Count != 0)
								incomming = inc;
						}
					}

					(IncommingCount, Commits) = incomming == null ? (0, commits) : (incomming.Count, incomming.Concat(commits));
					return;
				}
			}
			Commits = null;
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

		class CommitComparer : IEqualityComparer<ICommit>
		{
			internal static CommitComparer Instance { get; } = new CommitComparer();

			private CommitComparer()
			{ }

			public bool Equals(ICommit? x, ICommit? y)
				=> x != null && y != null && x.Equals(y);

			public int GetHashCode([DisallowNull] ICommit obj)
				=> obj.Sha.GetHashCode();
		}
	}
}

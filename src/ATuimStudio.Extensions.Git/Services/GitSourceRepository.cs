using ATuimStudio.Extensions.Core;
using LibGit2Sharp;

namespace ATuimStudio.Extensions.Git
{
	class GitSourceRepository : ISourceRepository
	{
		readonly Repository _repo;
		readonly ITimeProvider _timeProvider;

		public GitSourceRepository(string path, ITimeProvider timeProvider)
		{
			_repo = new Repository(path);
			_timeProvider = timeProvider;
		}

		public void Dispose()
		{
			_repo.Dispose();
		}

		public IEnumerable<IBranch> GetBranches()
			=> _repo.Branches
				.Where(static x => !x.IsRemote || x.CanonicalName != $"refs/remotes/{x.RemoteName}/HEAD")
				.Select(static x => new Branch(x));

		public string GetCurrentBranch()
			=> _repo.Head.FriendlyName;

		public IEnumerable<IFileStatus> GetFilesStatus()
			=> _repo.RetrieveStatus().Select(static x => new FileStatus(x.FilePath, (ATuimStudio.Extensions.Git.FileStatus)x.State));

		public IEnumerable<ICommit> GetCommits(IBranch branch)
		{
			if (branch is not Branch b)
				throw new InvalidOperationException("Invalid branch.");

			return CommitItem.MapCollection(_repo.Commits.QueryBy(new CommitFilter
			{
				SortBy = CommitSortStrategies.Topological,
				IncludeReachableFrom = b.Inner
			}));
		}

		Branch? _head;
		public IBranch Head
		{
			get
			{
				if (_head == null || _head.Inner != _repo.Head)
					_head = new Branch(_repo.Head);
				return _head;
			}
		}

		public void Checkout(string? @ref, string path)
			=> _repo.CheckoutPaths(@ref ?? _repo.Head.FriendlyName, [path], new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });

		public void Stage(string path)
			=> Commands.Stage(_repo, path == "" ? "*" : path);

		public void Unstage(string path)
			=> Commands.Unstage(_repo, path == "" ? "*" : path);

		readonly string[] _configUserParts = ["user", ""];
		public ICommit Commit(string message, bool amend)
		{
			string[] parts = _configUserParts;
			parts[1] = "name";
			string name = _repo.Config.Get<string>(parts).Value;
			parts[1] = "email";
			string email = _repo.Config.Get<string>(parts).Value;

			LibGit2Sharp.Signature sig = new LibGit2Sharp.Signature(name, email, _timeProvider.UtcNow);
			return new CommitItem(_repo.Commit(message, sig, sig, new CommitOptions { AmendPreviousCommit = amend }));
		}

		sealed class Branch : IBranch
		{
			readonly LibGit2Sharp.Branch _inner;
			internal Branch(LibGit2Sharp.Branch inner)
			{
				_inner = inner;
			}

			internal LibGit2Sharp.Branch Inner => _inner;

			public string Name => _inner.FriendlyName;
			public bool IsRemote => _inner.IsRemote;

			bool _remoteTaken;
			IBranch? _remote;
			public IBranch? GetRemote()
			{
				if (!_remoteTaken)
				{
					LibGit2Sharp.Branch? remote = _inner.TrackedBranch;
					_remote = remote == null ? null : remote == _inner ? this : new Branch(remote);
					_remoteTaken = true;
				}
				return _remote;
			}

			ICommit? _tip;
			public ICommit Tip => _tip ??= new CommitItem(_inner.Tip);

			public bool Equals(IBranch? other)
				=> other is Branch b && b._inner.CanonicalName.Equals(_inner.CanonicalName);

			public override bool Equals(object? obj)
				=> Equals(obj as IBranch);

			public override int GetHashCode()
				=> _inner.GetHashCode();
		}

		sealed class CommitItem : ICommit
		{
			readonly LibGit2Sharp.Commit _inner;
			internal CommitItem(LibGit2Sharp.Commit inner)
			{
				_inner = inner;
			}

			internal static IEnumerable<ICommit> MapCollection(IEnumerable<Commit> commits)
				=> commits.Select(static x => new CommitItem(x));

			public bool Equals(ICommit? other)
				=> other is CommitItem c && c._inner.Sha.EqualsOrdinal(_inner.Sha);

			public override bool Equals(object? obj)
				=> Equals(obj as ICommit);

			public override int GetHashCode()
				=> _inner.GetHashCode();

			public string Sha => _inner.Sha;
			public string Message => _inner.Message;
			public DateTimeOffset When => _inner.Committer.When;
			ISignature? _author;
			public ISignature Author => _author ??= new Signature(_inner.Author.Name, _inner.Author.Email);

			IReadOnlyList<ICommit>? _parents;
			public IReadOnlyList<ICommit> Parents => _parents ??= [.. MapCollection(_inner.Parents)];
		}

		sealed record Signature(string Name, string Email) : ISignature;

		sealed record FileStatus(string Path, ATuimStudio.Extensions.Git.FileStatus Status) : IFileStatus;
	}
}
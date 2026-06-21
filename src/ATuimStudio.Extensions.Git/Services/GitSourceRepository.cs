using ATuimStudio.Extensions.Core;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace ATuimStudio.Extensions.Git
{
	partial class GitSourceRepository : ISourceRepository
	{
		readonly Repository _repo;
		readonly ITimeProvider _timeProvider;
		readonly CredentialsHandler? _credentialsHandler;

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

		public bool Checkout(IBranch branch)
		{
			if (branch is not Branch b)
				throw new InvalidOperationException("Invalid implementation.");
			try
			{
				Commands.Checkout(_repo, b.Inner);
			}
			catch (CheckoutConflictException)
			{
				return false;
			}
			return true;
		}

		static string SanitizePath(string path)
			=> path == "" ? "*" : path;

		public void Checkout(string? @ref, string path)
			=> _repo.CheckoutPaths(@ref ?? _repo.Head.FriendlyName, [SanitizePath(path)], new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });

		public void Stage(string path)
			=> Commands.Stage(_repo, SanitizePath(path));

		public void Unstage(string path)
			=> Commands.Unstage(_repo, SanitizePath(path));

		public ICommit Commit(string message, bool amend)
		{
			LibGit2Sharp.Signature sig = CreateDefaultSignature();
			return new CommitItem(_repo.Commit(message, sig, sig, new CommitOptions { AmendPreviousCommit = amend }));
		}

		readonly string[] _configUserParts = ["user", ""];
		(string name, string email) GetDefaultIdetity()
		{
			string[] parts = _configUserParts;
			parts[1] = "name";
			string name = _repo.Config.Get<string>(parts).Value;
			parts[1] = "email";
			string email = _repo.Config.Get<string>(parts).Value;
			return (name, email);
		}

		LibGit2Sharp.Signature CreateDefaultSignature()
		{
			(string name, string email) = GetDefaultIdetity();
			return new LibGit2Sharp.Signature(name, email, _timeProvider.UtcNow);
		}

		LibGit2Sharp.Identity CreateDefaultIdentity()
		{
			(string name, string email) = GetDefaultIdetity();
			return new LibGit2Sharp.Identity(name, email);
		}

		#region remote
		public bool IsRemoteAvailable => !_repo.Info.IsHeadDetached && _repo.Head.IsTracking && _repo.Head.RemoteName is not null;

		public void Fetch()
		{
			Remote remote = GetTrackedRemote();
			IEnumerable<string> refSpecs = remote.FetchRefSpecs.Select(static x => x.Specification);
			Commands.Fetch(_repo, remote.Name, refSpecs, CreateFetchOptions(), string.Empty);
		}

		public IMergeResult Pull()
		{
			EnsureRemoteAvailable();
			PullOptions options = new PullOptions { FetchOptions = CreateFetchOptions() };
			return new MergeResult(Commands.Pull(_repo, CreateDefaultSignature(), options));
		}

		public IRebaseResult Rebase()
		{
			EnsureRemoteAvailable();
			LibGit2Sharp.Branch head = _repo.Head;
			LibGit2Sharp.Branch upstream = head.TrackedBranch;
			RebaseOptions options = new RebaseOptions();
			return new RebaseResult(_repo.Rebase.Start(head, upstream, null, CreateDefaultIdentity(), options));
		}

		public void Push()
		{
			var remote = GetTrackedRemote();
			_repo.Network.Push(remote, _repo.Head.CanonicalName, CreatePushOptions());
		}

		Remote GetTrackedRemote()
		{
			EnsureRemoteAvailable();
			return _repo.Network.Remotes[_repo.Head.RemoteName];
		}

		void EnsureRemoteAvailable()
		{
			if (!IsRemoteAvailable)
				throw new InvalidOperationException(
					 "HEAD does not track a remote branch. Remote operations are not available.");
		}

		FetchOptions CreateFetchOptions()
		{
			FetchOptions options = new FetchOptions();
			ApplyCredentials(options);
			return options;
		}

		PushOptions CreatePushOptions()
		{
			PushOptions options = new PushOptions();
			ApplyCredentials(options);
			return options;
		}

		void ApplyCredentials(FetchOptionsBase options)
		{
			if (_credentialsHandler is not null)
				options.CredentialsProvider = _credentialsHandler;
		}

		void ApplyCredentials(PushOptions options)
		{
			if (_credentialsHandler is not null)
				options.CredentialsProvider = _credentialsHandler;
		}
		#endregion remote

		#region interface implementations
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

		sealed class MergeResult : IMergeResult
		{
			readonly LibGit2Sharp.MergeResult _inner;
			internal MergeResult(LibGit2Sharp.MergeResult inner)
			{
				_inner = inner;
			}

			public MergeStatus Status => (MergeStatus)_inner.Status;
		}

		sealed class RebaseResult : IRebaseResult
		{
			readonly LibGit2Sharp.RebaseResult _inner;
			internal RebaseResult(LibGit2Sharp.RebaseResult inner)
			{
				_inner = inner;
			}
		}
		#endregion interface implementations
	}
}
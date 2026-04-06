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
			=> _repo.Branches.Select(static x => new Branch(x));

		public string GetCurrentBranch()
			=> _repo.Head.FriendlyName;

		public IEnumerable<IFileStatus> GetFilesStatus()
			=> _repo.RetrieveStatus().Select(static x => new FileStatus(x.FilePath, (ATuimStudio.Extensions.Git.FileStatus)x.State));

		public IEnumerable<ICommit> GetCommits(IBranch branch)
		{
			if (branch is not Branch b)
				throw new InvalidOperationException("Invalid branch.");

			return b.Inner.Commits.Select(static x => new CommitItem(x));
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

			Signature sig = new Signature(name, email, _timeProvider.UtcNow);
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
		}

		sealed class CommitItem : ICommit
		{
			readonly LibGit2Sharp.Commit _inner;
			internal CommitItem(LibGit2Sharp.Commit inner)
			{
				_inner = inner;
			}

			public string Sha => _inner.Sha;
			public string Message => _inner.Message;
			public DateTimeOffset When => _inner.Committer.When;
		}

		sealed record FileStatus(string Path, ATuimStudio.Extensions.Git.FileStatus Status) : IFileStatus;
	}
}
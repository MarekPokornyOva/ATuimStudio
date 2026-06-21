namespace ATuimStudio.Extensions.Git
{
	public interface ISourceRepository : IDisposable
	{
		IBranch Head { get; }
		IEnumerable<IBranch> GetBranches();
		IEnumerable<IFileStatus> GetFilesStatus();
		IEnumerable<ICommit> GetCommits(IBranch branch);
		bool Checkout(IBranch branch);
		void Checkout(string? @ref, string path);
		void Stage(string path);
		void Unstage(string path);
		string GetCurrentBranch();
		ICommit Commit(string commitMessage, bool amend);

		void Fetch();
		IMergeResult Pull();
		IRebaseResult Rebase();
		void Push();
	}
}

namespace ATuimStudio.Extensions.Git
{
	public interface ISourceRepository : IDisposable
	{
		IEnumerable<IBranch> GetBranches();
		IEnumerable<IFileStatus> GetFilesStatus();
		IEnumerable<ICommit> GetCommits(IBranch branch);
		void Checkout(string? @ref, string path);
		void Stage(string path);
		void Unstage(string path);
		string GetCurrentBranch();
		ICommit Commit(string commitMessage, bool amend);
	}
}

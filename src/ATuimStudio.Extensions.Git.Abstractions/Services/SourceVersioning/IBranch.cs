namespace ATuimStudio.Extensions.Git
{
	public interface IBranch : IEquatable<IBranch>
	{
		string Name { get; }
		bool IsRemote { get; }
		
		IBranch? GetRemote();
		ICommit Tip { get; }
	}
}

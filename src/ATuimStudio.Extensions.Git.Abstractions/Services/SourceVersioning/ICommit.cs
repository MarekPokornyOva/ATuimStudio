namespace ATuimStudio.Extensions.Git
{
	public interface ICommit
	{
		string Sha { get; }
		string Message { get; }
		DateTimeOffset When { get; }
	}
}

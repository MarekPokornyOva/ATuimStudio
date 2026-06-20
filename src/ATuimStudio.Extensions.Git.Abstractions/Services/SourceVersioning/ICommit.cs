namespace ATuimStudio.Extensions.Git
{
	public interface ICommit : IEquatable<ICommit>
	{
		string Sha { get; }
		string Message { get; }
		DateTimeOffset When { get; }
		ISignature Author { get; }
		IReadOnlyList<ICommit> Parents { get; }
	}
}

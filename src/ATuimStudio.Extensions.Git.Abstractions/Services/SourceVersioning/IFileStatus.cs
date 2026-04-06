namespace ATuimStudio.Extensions.Git
{
	public interface IFileStatus
	{
		string Path { get; }
		FileStatus Status { get; }
	}
}

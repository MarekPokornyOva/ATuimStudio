namespace ATuimStudio.Extensions.FindInFiles
{
	public interface IFindInFilesRequest
	{
		string SearchText { get; }
		bool UseRegex { get; }
	}
}

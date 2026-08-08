namespace ATuimStudio.Extensions.FindInFiles
{
	public interface IFindInFiles
	{
		IAsyncEnumerable<TextSearchResult> FindTextAsync(string searchText, bool useRegex, CancellationToken cancellationToken);
	}

	public record struct TextSearchResult
		(
			string FilePath,
			int LineNumber,
			int ColumnNumber,
			string LineText
		);
}

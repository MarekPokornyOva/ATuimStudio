namespace ATuimStudio.Extensions.FindInFiles
{
	public interface IFindInFiles
	{
		IAsyncEnumerable<TextSearchResult> FindTextAsync(string searchText, bool useRegex, CancellationToken cancellationToken);
		IAsyncEnumerable<TextSearchResult> ReplaceTextAsync(string searchText, bool useRegex, string replaceText, CancellationToken cancellationToken);
	}

	public record struct TextSearchResult
		(
			string FilePath,
			string LineText,
			int LineNumber,
			int ColumnNumber,
			int Offset,
			int FoundLength
		);
}

namespace ATuimStudio.Extensions.TextEditReferences
{
	public interface IAllReferencesResult
	{
		IReadOnlyCollection<ReferenceItem> References { get; }
	}

	public record struct ReferenceItem(string FilePath, int Position, int Line, int Column);
}

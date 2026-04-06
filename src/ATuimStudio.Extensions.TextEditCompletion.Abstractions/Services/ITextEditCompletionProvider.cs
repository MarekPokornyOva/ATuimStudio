namespace ATuimStudio.Extensions.TextEditCompletion
{
	public interface ITextEditCompletionProvider
	{
		Task<IReadOnlyCollection<ITextEditCompletionItem>> GetCompletions(string path, int position, CancellationToken cancellationToken);
	}
}

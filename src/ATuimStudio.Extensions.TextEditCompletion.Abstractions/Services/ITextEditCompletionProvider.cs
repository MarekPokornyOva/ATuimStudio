namespace ATuimStudio.Extensions.TextEditCompletion
{
	public interface ITextEditCompletionProvider
	{
		Task<ITextEditCompletionResult> GetCompletions(string path, int position, CancellationToken cancellationToken);
	}
}

namespace ATuimStudio.Extensions.TextEditCompletion
{
	public interface ITextEditCompletionProvider
	{
		Task<ITextEditCompletionResult> GetCompletionsAsync(string path, int position, CancellationToken cancellationToken);
	}
}

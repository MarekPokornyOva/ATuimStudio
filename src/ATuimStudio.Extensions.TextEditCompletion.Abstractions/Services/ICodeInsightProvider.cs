namespace ATuimStudio.Extensions.TextEditCompletion
{
	public interface ICodeInsightProvider
	{
		Task<ICodeInsightResult> GetAsync(string path, int position, CancellationToken cancellationToken);
	}
}

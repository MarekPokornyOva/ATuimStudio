namespace ATuimStudio.Extensions.TextEditCompletion
{
	public interface ICodeInsightProvider
	{
		Task<ICodeInsightResult> Get(string path, int position, CancellationToken cancellationToken);
	}
}

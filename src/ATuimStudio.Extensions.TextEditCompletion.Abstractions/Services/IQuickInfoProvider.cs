namespace ATuimStudio.Extensions.TextEditCompletion
{
	public interface IQuickInfoProvider
	{
		Task<IQuickInfoResult?> Get(string path, int position, CancellationToken cancellationToken);
	}
}

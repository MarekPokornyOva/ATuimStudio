namespace ATuimStudio.Extensions.TextEditReferences
{
	public interface IQuickInfoProvider
	{
		Task<IQuickInfoResult?> GetAsync(string path, int position, CancellationToken cancellationToken);
	}
}

namespace ATuimStudio.Extensions.Core
{
	public interface ISolutionDataProvider
	{
		Task<ISolutionData> GetSolutionDataAsync(string path, CancellationToken cancellationToken);
	}
}

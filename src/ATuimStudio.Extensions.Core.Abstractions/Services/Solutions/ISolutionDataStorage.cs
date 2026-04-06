namespace ATuimStudio.Extensions.Core
{
	public interface ISolutionDataStorage : ISolutionDataProvider
	{
		void DeleteFile(string path);
		Task SaveDocumentAsync(string path, string content, CancellationToken cancellationToken);
	}
}

namespace ATuimStudio.Extensions.Core.Ui
{
	public interface IUiDocumentService
	{
		IProjectInfo? GetActiveDocumentProject();
		Task SaveCurrentDocument(CancellationToken cancellationToken);
		Task SaveAllOpenedDocuments(CancellationToken cancellationToken);
	}

	public interface IProjectInfo
	{
		string Name { get; }
	}
}

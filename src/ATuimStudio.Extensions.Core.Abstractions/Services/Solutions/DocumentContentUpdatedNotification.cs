namespace ATuimStudio.Extensions.Core
{
	public record struct DocumentContentUpdatedNotification
	(
		string FilePath,
		DocumentUpdateInfo DocumentUpdateInfo
	);
}

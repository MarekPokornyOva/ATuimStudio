using Avalonia.Controls;

namespace ATuimStudio.Extensions.Core.Ui
{
	public interface IUiDocumentService
	{
		IProjectInfo? GetActiveDocumentProject();
		Task SaveCurrentDocument(CancellationToken cancellationToken);
		Task SaveAllOpenedDocuments(CancellationToken cancellationToken);
		void AddSpecialDocument(string id, string title, Func<IServiceProvider, object> viewModelFactory, Func<IServiceProvider, Control> viewFactory);
	}

	public interface IProjectInfo
	{
		string Name { get; }
	}
}

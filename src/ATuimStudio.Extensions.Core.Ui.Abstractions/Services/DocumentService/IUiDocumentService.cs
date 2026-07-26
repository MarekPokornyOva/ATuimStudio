using Avalonia.Controls;

namespace ATuimStudio.Extensions.Core.Ui
{
	public interface IUiDocumentService
	{
		void SetActiveDocument(string path);
		void SetActiveDocument(string path, int line, int? column);
		void SetActiveDocument(string path, int offset);
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

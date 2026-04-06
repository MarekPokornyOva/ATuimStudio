namespace ATuimStudio.Extensions.Core
{
	public interface ISolutionService
	{
		Task LoadSolutionAsync(string path, CancellationToken cancellationToken);
		event Action<object?, SolutionLoadedEventArgs>? OnSolutionLoaded;
		void UnloadSolution();
		event Action<object?, SolutionUnloadingEventArgs>? OnSolutionUnloading;
		event Action<object?, SolutionUnloadedEventArgs>? OnSolutionUnloaded;
		Task SaveDocumentAsync(string path, string content, CancellationToken cancellationToken);
		string? GetDocumentContent(string path);
		void UpdateDocumentContent(string path, DocumentUpdateInfo documentUpdateInfo);
		IProjectFileData? CreateFile(string path);
		void DeleteFile(string path);

		ISolutionData? CurrentSolution { get; }
	}
}

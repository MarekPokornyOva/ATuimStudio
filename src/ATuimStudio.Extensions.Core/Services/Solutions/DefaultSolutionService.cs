using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ATuimStudio.Extensions.Core
{
	sealed class DefaultSolutionService : ISolutionService
	{
		public event Action<object?, SolutionLoadedEventArgs>? OnSolutionLoaded;
		public event Action<object?, SolutionUnloadingEventArgs>? OnSolutionUnloading;
		public event Action<object?, SolutionUnloadedEventArgs>? OnSolutionUnloaded;

		public ISolutionData? CurrentSolution { get; private set; }
		Solution? _rawSolution;

		readonly ISolutionDataStorage _solutionDataStorage;
		public DefaultSolutionService(ISolutionDataStorage solutionDataStorage)
		{
			_solutionDataStorage = solutionDataStorage;
		}

		public async Task LoadSolutionAsync(string path, CancellationToken cancellationToken)
		{
			UnloadSolution();

			ISolutionData solutionData = await _solutionDataStorage.GetSolutionDataAsync(path, cancellationToken);
			CurrentSolution = solutionData;
			_rawSolution = solutionData.RawData as Solution;
			OnSolutionLoaded?.Invoke(null!, new SolutionLoadedEventArgs(solutionData));
		}

		public void UnloadSolution()
		{
			ISolutionData? currentSolution = CurrentSolution;
			if (currentSolution == null)
				return;

			OnSolutionUnloading?.Invoke(null!, new SolutionUnloadingEventArgs(currentSolution));
			CurrentSolution = null;
			_rawSolution = null;
			OnSolutionUnloaded?.Invoke(null!, SolutionUnloadedEventArgs.Instance);
		}

		public Task SaveDocumentAsync(string path, string content, CancellationToken cancellationToken)
			=> _solutionDataStorage.SaveDocumentAsync(path, content, cancellationToken);

		void UpdateRawSolution(Solution rawSolution)
		{
			_rawSolution = rawSolution;
			CurrentSolution!.RawData = rawSolution;
		}

		public string? GetDocumentContent(string path)
			=> GetDocumentSource(path)?.SourceText.ToString();

		public void UpdateDocumentContent(string path, DocumentUpdateInfo documentUpdateInfo)
		{
			(Document Document, SourceText SourceText)? docInfo = GetDocumentSource(path);
			if (docInfo == null)
				return;
			(Document msDoc, SourceText sourceText) = docInfo.Value;
			sourceText = sourceText.WithChanges(new TextChange(new TextSpan(documentUpdateInfo.Position, documentUpdateInfo.RemovedText.Length), documentUpdateInfo.InsertedText));
			Document newDocument = msDoc.WithText(sourceText);
			UpdateRawSolution(newDocument.Project.Solution);
		}

		Document? FindDocument(string path)
		{
			if (_rawSolution == null)
				return null;
			return _rawSolution.Projects.SelectMany(static p => p.Documents).FirstOrDefault(d => d.FilePath.EqualsOrdinal(path));
		}
		(Document Document, SourceText SourceText)? GetDocumentSource(string path)
		{
			Document? msDoc = FindDocument(path);
			if (msDoc == null)
				return null;
			return (msDoc, msDoc.GetTextAsync().GetAwaiter().GetResult());
		}

		public void DeleteFile(string path)
		{
			Document? msDoc = FindDocument(path);
			if (msDoc == null)
				return;
			UpdateRawSolution(_rawSolution!.RemoveDocument(msDoc.Id));
			_solutionDataStorage.DeleteFile(path);
		}

		Project? FindProject(string path)
		{
			if (_rawSolution == null)
				return null;
			return _rawSolution.Projects.FirstOrDefault(d => PathHelper.IsRootPathOf(Path.GetDirectoryName(d.FilePath)!, path));
		}

		public IProjectFileData? CreateFile(string path)
		{
			Project? project = FindProject(path);
			if (project == null)
				return null;
			string filename = Path.GetFileName(path);
			UpdateRawSolution(_rawSolution!.AddDocument(DocumentId.CreateNewId(project.Id), filename, "", filePath: path));
			return new PhysicalDiskSolutionDataStorage.ProjectFileData(filename, path);
		}
	}
}

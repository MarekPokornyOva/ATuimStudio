using ATuimStudio.Extensions.Core;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATuimStudio.ViewModels
{
	public sealed class DocumentViewModel : ObservableObject, IDisposable
	{
		readonly ISolutionService _solutionService;
		readonly ICodeDiagnosticsManager _codeDiagnosticsManager;
		public DocumentViewModel(ISolutionService solutionService, ICodeDiagnosticsManager codeDiagnosticsManager)
		{
			_solutionService = solutionService;
			_codeDiagnosticsManager = codeDiagnosticsManager;
		}

		public IDocument? FileContent { get; set; }

		IProjectFileData? _fileData;
		internal IProjectFileData? FileData { get => _fileData; set { _fileData = value; LoadFile(value); } }
		
		void LoadFile(IProjectFileData? fileData)
		{
			if (fileData == null)
				FileContent = null;
			else
			{
				string path = fileData.Path;
				string source = _solutionService.GetDocumentContent(path)
					?? ""/*File.ReadAllText(fileData.Path)*/;

				TextDocument textDocument = new TextDocument(new StringTextSource(source)) { FileName = path };
				textDocument.Changed += TextDocument_Changed;
				FileContent = textDocument;

				_diagRefreshTimer = new Timer(DiagRefresh, path, Timeout.Infinite, Timeout.Infinite);
				_codeDiagnosticsManager.Refresh(path);
			}
		}

		Timer? _diagRefreshTimer;
		private void TextDocument_Changed(object? sender, DocumentChangeEventArgs e)
		{
			//TextDocument textDocument = (TextDocument)sender!;
			if (_fileData == null)
				return;

			string path = _fileData.Path;
			_solutionService.UpdateDocumentContent(path, new DocumentUpdateInfo(e.Offset, e.InsertedText.Text, e.RemovedText.Text));

			_diagRefreshTimer?.Change(1000, Timeout.Infinite);
		}

		void DiagRefresh(object? state)
		{
			_codeDiagnosticsManager.Refresh((string)state!);
		}

		public void Dispose()
		{
			_diagRefreshTimer?.Dispose();
		}
	}
}

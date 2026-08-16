using ATuimStudio.Extensions.Core;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATuimStudio.ViewModels
{
	sealed partial class DocumentViewModel : ObservableObject, IDisposable
	{
		readonly ISolutionService _solutionService;
		readonly ICodeDiagnosticsManager _codeDiagnosticsManager;
		readonly ISub<DocumentContentUpdatedNotification> _subDocumentContentUpdatedNotification;
		public DocumentViewModel(ISolutionService solutionService, ICodeDiagnosticsManager codeDiagnosticsManager, ISub<DocumentContentUpdatedNotification> subDocumentContentUpdatedNotification)
		{
			_solutionService = solutionService;
			_codeDiagnosticsManager = codeDiagnosticsManager;
			_subDocumentContentUpdatedNotification = subDocumentContentUpdatedNotification;

			subDocumentContentUpdatedNotification.Register(HandleDocumentContentUpdated);
		}

		[ObservableProperty]
		public partial IDocument? FileContent { get; private set; }

		IProjectFileData? _fileData;
		internal IProjectFileData? FileData { get => _fileData; set { _fileData = value; LoadFile(value); } }
		
		void LoadFile(IProjectFileData? fileData)
		{
			if (fileData == null)
				FileContent = null;
			else
				LoadFile(fileData.Path);
		}

		void LoadFile(string path)
		{
			string source = _solutionService.GetDocumentContent(path)
				?? ""/*File.ReadAllText(fileData.Path)*/;

			TextDocument textDocument = new TextDocument(new StringTextSource(source)) { FileName = path };
			textDocument.Changed += TextDocument_Changed;
			FileContent = textDocument;

			_diagRefreshTimer = new Timer(DiagRefresh, path, Timeout.Infinite, Timeout.Infinite);
			_codeDiagnosticsManager.Refresh(path);
		}

		bool isChanging = false;
		Timer? _diagRefreshTimer;
		private void TextDocument_Changed(object? sender, DocumentChangeEventArgs e)
		{
			//TextDocument textDocument = (TextDocument)sender!;
			if (_fileData == null)
				return;

			if (!isChanging)
			{
				isChanging = true;
				_solutionService.UpdateDocumentContent(_fileData.Path, new DocumentUpdateInfo(e.Offset, e.InsertedText.Text, e.RemovedText.Text));
				isChanging = false;
			}

			IsDirty = true;

			_diagRefreshTimer?.Change(1000, Timeout.Infinite);
		}

		[ObservableProperty]
		public partial bool IsDirty { get; private set; }

		void DiagRefresh(object? state)
		{
			_codeDiagnosticsManager.Refresh((string)state!);
		}

		void HandleDocumentContentUpdated(DocumentContentUpdatedNotification notification)
		{
			if (isChanging)
				return;


			string path = notification.FilePath;
			if (path == FileData?.Path)
			{
				isChanging = true;
				DocumentUpdateInfo documentUpdateInfo = notification.DocumentUpdateInfo;
				FileContent?.Replace(documentUpdateInfo.Position, documentUpdateInfo.RemovedText.Length, documentUpdateInfo.InsertedText);
				isChanging = false;
			}
		}

		public void Dispose()
		{
			_diagRefreshTimer?.Dispose();
			_subDocumentContentUpdatedNotification.Unregister(HandleDocumentContentUpdated);
		}

		internal void UnmarkDirty()
		{
			IsDirty = false;
		}
	}
}

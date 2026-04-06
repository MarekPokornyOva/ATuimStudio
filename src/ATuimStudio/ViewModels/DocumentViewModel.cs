using ATuimStudio.Extensions.Core;
using AvaloniaEdit.Document;
using Dock.Model.Mvvm.Controls;

namespace ATuimStudio.ViewModels
{
	public class DocumentViewModel : Document
	{
		readonly ISolutionService _solutionService;
		public DocumentViewModel(ISolutionService solutionService)
		{
			_solutionService = solutionService;
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
				string source = _solutionService.GetDocumentContent(fileData.Path)
					?? ""/*File.ReadAllText(fileData.Path)*/;

				TextDocument textDocument = new TextDocument(new StringTextSource(source)) { FileName = fileData.Path };
				textDocument.Changed += TextDocument_Changed;
				FileContent = textDocument;
			}
		}

		private void TextDocument_Changed(object? sender, DocumentChangeEventArgs e)
		{
			//TextDocument textDocument = (TextDocument)sender!;
			if (_fileData == null)
				return;

			_solutionService.UpdateDocumentContent(_fileData.Path, new DocumentUpdateInfo(e.Offset, e.InsertedText.Text, e.RemovedText.Text));
		}
	}
}

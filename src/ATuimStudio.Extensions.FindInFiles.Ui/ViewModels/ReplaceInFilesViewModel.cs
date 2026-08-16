using ATuimStudio.Extensions.Core.Ui;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATuimStudio.Extensions.FindInFiles
{
	partial class ReplaceInFilesViewModel : ObservableObject
	{
		readonly Window _dialogWindow;
		readonly IFindInFiles _findInFiles;
		readonly IUiDocumentService _uiDocumentService;
		public ReplaceInFilesViewModel(Window dialogWindow, string? defaultSearchText, IFindInFiles findInFiles, IUiDocumentService uiDocumentService)
		{
			_dialogWindow = dialogWindow;
			SearchText = defaultSearchText ?? "";
			_findInFiles = findInFiles;
			_uiDocumentService = uiDocumentService;
		}

		public string SearchText { get; set; }
		public string? ReplaceText { get; set; }
		public bool UseRegex { get; set; }

		[RelayCommand]
		async Task Ok(CancellationToken cancellationToken)
		{
			if (!string.IsNullOrEmpty(SearchText) && SearchText != ReplaceText)
				await foreach (TextSearchResult item in _findInFiles.ReplaceTextAsync(SearchText, UseRegex, ReplaceText ?? "", cancellationToken))
					_uiDocumentService.SetActiveDocument(item.FilePath, item.LineNumber, item.ColumnNumber);
			_dialogWindow.Close();
		}

		[RelayCommand]
		void Cancel()
		{
			_dialogWindow.Close();
		}
	}
}

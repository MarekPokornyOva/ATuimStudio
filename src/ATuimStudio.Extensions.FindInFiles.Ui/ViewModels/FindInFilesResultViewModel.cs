using ATuimStudio.Extensions.Core;
using ATuimStudio.Extensions.Core.Ui;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ATuimStudio.Extensions.FindInFiles
{
	partial class FindInFilesResultViewModel : IDisposable
	{
		readonly ISub<IFindInFilesRequest> _subFindInFilesRequest;
		readonly IFindInFiles _findInFiles;
		readonly IUiDocumentService _documentService;
		CancellationTokenSource? _cts;
		public FindInFilesResultViewModel(ISub<IFindInFilesRequest> subFindInFilesRequest, IFindInFiles findInFiles, IUiDocumentService documentService)
		{
			_subFindInFilesRequest = subFindInFilesRequest;
			_findInFiles = findInFiles;
			_documentService = documentService;

			subFindInFilesRequest.Register(HandleRequest);
		}

		public ObservableCollection<TextSearchResult> Items { get; } = [];

		async void HandleRequest(IFindInFilesRequest request)
		{
			_cts?.Cancel();
			_cts = new CancellationTokenSource();
			Items.Clear();

			await foreach (TextSearchResult item in _findInFiles.FindTextAsync(request.SearchText, request.UseRegex, _cts.Token))
				Items.Add(item);
		}
		
		[RelayCommand]
		void RowDoubleClick(TextSearchResult parameter)
		{
			_documentService.SetActiveDocument(parameter.FilePath, parameter.LineNumber, parameter.ColumnNumber);
		}

		public void Dispose()
		{
			_subFindInFilesRequest.Unregister(HandleRequest);
			if (_cts != null)
			{
				_cts.Cancel();
				_cts.Dispose();
			}
		}
	}
}

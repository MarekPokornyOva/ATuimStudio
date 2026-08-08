using ATuimStudio.Extensions.Core;
using ATuimStudio.Extensions.Core.Ui;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATuimStudio.Extensions.FindInFiles
{
	partial class FindInFilesViewModel : ObservableObject
	{
		readonly Window _dialogWindow;
		readonly IUiWindowService _uiWindowService;
		readonly IPub<IFindInFilesRequest> _pubFindInFilesRequest;
		public FindInFilesViewModel(Window dialogWindow, string? defaultSearchText, IUiWindowService uiWindowService, IPub<IFindInFilesRequest> pubFindInFilesRequest)
		{
			_dialogWindow = dialogWindow;
			SearchText = defaultSearchText ?? "";
			_uiWindowService = uiWindowService;
			_pubFindInFilesRequest = pubFindInFilesRequest;
		}

		public string SearchText { get; set; }
		public bool UseRegex { get; set; }

		[RelayCommand]
		void Ok()
		{
			if (!string.IsNullOrEmpty(SearchText))
				_uiWindowService.OpenPane(Extension.TypeFindInFilesResult, WellKnownLayoutConstants.IdBasicInfo, Extension.IdFindInFilesResult, $"Find \"{SearchText}\"")
					?.ContinueWith(_ => _pubFindInFilesRequest.Raise(new FindInFilesRequest(SearchText, UseRegex)));
			_dialogWindow.Close();
		}

		[RelayCommand]
		void Cancel()
		{
			_dialogWindow.Close();
		}

		record FindInFilesRequest(string SearchText, bool UseRegex) : IFindInFilesRequest;
	}
}

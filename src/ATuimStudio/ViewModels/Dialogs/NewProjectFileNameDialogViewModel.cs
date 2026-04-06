using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;

namespace ATuimStudio.ViewModels
{
	partial class NewProjectFileNameDialogViewModel : ViewModelBase
	{
		[EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable CS8618
		public NewProjectFileNameDialogViewModel()
		{ }
#pragma warning restore CS8618

		readonly Window _dialogWindow;
		readonly string _path;
		public NewProjectFileNameDialogViewModel(Window dialogWindow, string path)
		{
			_dialogWindow = dialogWindow;
			_path = path;
		}

		public string Path => _path;

		[ObservableProperty][NotifyCanExecuteChangedFor(nameof(OkCommand))] string _fileName = ".cs";

		[RelayCommand(CanExecute = nameof(FileNameIsValid))]
		void Ok()
		{
			_dialogWindow.Close(FileName);
		}

		[RelayCommand]
		void Cancel()
		{
			_dialogWindow.Close(null);
		}

		static readonly char[] _invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();
		bool FileNameIsValid()
			=> FileName.Length != 0 && !FileName.Any(static x => Array.IndexOf(_invalidFileNameChars, x) != -1);
	}
}

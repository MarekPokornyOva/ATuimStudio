using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;

namespace ATuimStudio.ViewModels
{
	partial class MessageBoxViewModel : ViewModelBase
	{
		[EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable CS8618
		public MessageBoxViewModel()
		{ }
#pragma warning restore CS8618

		readonly Window _dialogWindow;
		readonly string _message;
		public MessageBoxViewModel(Window dialogWindow, string message)
		{
			_dialogWindow = dialogWindow;
			_message = message;
		}

		public string Message => _message;

		[RelayCommand]
		void Ok()
		{
			_dialogWindow.Close();
		}
	}
}

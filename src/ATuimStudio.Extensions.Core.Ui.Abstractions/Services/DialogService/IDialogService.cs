namespace ATuimStudio.Extensions.Core.Ui
{
	public interface IDialogService
	{
		//https://github.com/AvaloniaUI/Avalonia/discussions/12551
		//https://stackoverflow.com/questions/75745013/avalonia-is-there-a-ready-solution-for-showing-dialogs-modal-in-desktop-and-w
		Task<TResult> OpenModal<TViewModel, TResult>(DialogWindowParameters dialogWindowParameters, object[] viewModelParameters);
		Task ShowMessage(string message);
	}
}

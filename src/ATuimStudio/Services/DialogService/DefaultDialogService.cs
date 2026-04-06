using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ATuimStudio.Views;
using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio
{
	sealed class DefaultDialogService : IDialogService
	{
		readonly IServiceProvider _serviceProvider;
		public DefaultDialogService(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		//Window _mainWindow = default!;
		//internal void SetMainWindow(Window mainWindow)
		//{
		//	_mainWindow = mainWindow;
		//}

		public Task<TResult> OpenModal<TViewModel, TResult>(DialogWindowParameters dialogWindowParameters, params object[] viewModelParameters)
		{
			if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
				throw new InvalidOperationException("No dialog service supported for the application type.");
			Window _mainWindow = desktop.MainWindow!;

			DialogWindow dialogWindow = new DialogWindow(dialogWindowParameters);
			int viewModelParametersLen = viewModelParameters.Length;
			Array.Resize(ref viewModelParameters, viewModelParametersLen + 1);
			viewModelParameters[viewModelParametersLen] = (Window)dialogWindow;
			object viewModel = ActivatorUtilities.CreateInstance<TViewModel>(_serviceProvider, viewModelParameters)
				?? throw new InvalidOperationException("Dialog view model can't be creted.");
			dialogWindow.Content = viewModel;
			return dialogWindow.ShowDialog<TResult>(_mainWindow);
		}
	}
}

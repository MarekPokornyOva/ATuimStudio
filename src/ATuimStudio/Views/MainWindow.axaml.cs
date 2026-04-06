using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows.Input;

namespace ATuimStudio.Views;

public partial class MainWindow : Window
{
	[EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable CS8618
	public MainWindow()
	{
		InitializeComponent();

		Loaded += MainWindow_Loaded;
	}
#pragma warning restore CS8618

	private void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
	{
		foreach (ILogical item in this.GetLogicalDescendants())
			if (item is MenuItem menuItem)
			{
				ICommand? command;
				KeyGesture? gesture = menuItem.InputGesture;
				if (gesture != null && (command = menuItem.Command) != null)
					KeyBindings.Add(new KeyBinding { Gesture = gesture, Command = command });
			}
	}

	readonly ServiceProvider _serviceProvider;
	public MainWindow(ServiceProvider serviceProvider) : this()
	{
		_serviceProvider = serviceProvider;
	}

	/// <inheritdoc/>
	protected override void OnClosed(EventArgs e)
	{
		_serviceProvider.Dispose();
		base.OnClosed(e);
	}
}

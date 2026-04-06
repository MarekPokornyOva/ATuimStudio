using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ATuimStudio.Views;

public partial class NewProjectFileNameDialogView : UserControl
{
	public NewProjectFileNameDialogView()
	{
		InitializeComponent();
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);
		tbFileName.Focus();
	}
}

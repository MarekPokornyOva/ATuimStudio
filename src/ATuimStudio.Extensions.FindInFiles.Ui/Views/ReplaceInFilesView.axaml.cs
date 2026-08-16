using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ATuimStudio.Extensions.FindInFiles;

public partial class ReplaceInFilesView : UserControl
{
	public ReplaceInFilesView()
	{
		InitializeComponent();
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);
		tbSearchText.Focus();
	}
}

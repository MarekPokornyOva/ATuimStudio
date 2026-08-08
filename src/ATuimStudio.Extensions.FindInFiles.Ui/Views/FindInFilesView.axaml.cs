using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ATuimStudio.Extensions.FindInFiles;

public partial class FindInFilesView : UserControl
{
	public FindInFilesView()
	{
		InitializeComponent();
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);
		tbSearchText.Focus();
	}
}

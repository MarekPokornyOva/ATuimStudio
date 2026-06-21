using ATuimStudio.Extensions.Core.Ui;
using Avalonia.Controls;
using System.ComponentModel;

namespace ATuimStudio.Views;

public partial class DialogWindow : Window
{
	[EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable CS8618
	public DialogWindow()
	{
		InitializeComponent();
	}
#pragma warning restore CS8618


	public DialogWindow(DialogWindowParameters parameters) : this()
	{
		this.Title = parameters.Title;
	}
}

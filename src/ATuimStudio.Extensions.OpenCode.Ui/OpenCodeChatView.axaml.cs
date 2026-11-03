using Avalonia.Controls;
using Avalonia.Input;

namespace ATuimStudio.Extensions.OpenCode;

public partial class OpenCodeChatView : UserControl
{
	public OpenCodeChatView()
	{
		InitializeComponent();

		Prompt.KeyDown += Prompt_KeyDown;
	}

	private void Prompt_KeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key != Key.Enter && e.Key != Key.Return)
			return;

		// CTRL+ENTER: send the prompt
		if ((e.KeyModifiers & KeyModifiers.Control) == KeyModifiers.Control)
		{
			e.Handled = true;
			if (DataContext is OpenCodeChatViewModel viewModel)
				viewModel.SubmitCommand.Execute(Prompt.Text);
			return;
		}

		// ENTER alone: insert a new line at the caret position
		e.Handled = true;
		var caret = Prompt.CaretIndex;
		var text = Prompt.Text ?? string.Empty;
		text = text.Insert(caret, Environment.NewLine);
		Prompt.Text = text;
		Prompt.CaretIndex = caret + Environment.NewLine.Length;
	}
}

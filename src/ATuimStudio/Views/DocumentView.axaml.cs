using Avalonia.Controls;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using AvaloniaEdit.Editing;
using ATuimStudio.Services;
using System.ComponentModel;
using ATuimStudio.Extensibility;
using AvaloniaEdit;
using Avalonia;

namespace ATuimStudio.Views;

public partial class DocumentView : UserControl
{
	readonly IServiceProvider _serviceProvider;
	[EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable CS8618
	public DocumentView()
	{
		InitializeComponent();
	}
#pragma warning restore CS8618

	public DocumentView(IPluginPartsRegistrator pluginPartsRegistrator, IServiceProvider serviceProvider) : this()
	{
		_serviceProvider = serviceProvider;

		//Initial setup of TextMate.
		RegistryOptions registryOptions = new RegistryOptions(ThemeName.DarkPlus);
		TextMate.Installation textMateInstallation = Editor.InstallTextMate(registryOptions);
		textMateInstallation.SetGrammar(registryOptions.GetScopeByLanguageId("csharp"));

		//Set Caret position on mouse right click
		Editor.TextArea.ContextRequested += static (sender, e) =>
		{
			TextArea textArea = (TextArea)sender!;
			double leftMarginsWidth = textArea.LeftMargins.Count == 0 ? 0 : textArea.LeftMargins[^1].Bounds.Right;
			if (!e.TryGetPosition(textArea, out Point point))
				return;
			TextViewPosition? position = textArea.TextView.GetPositionFloor(point + textArea.TextView.ScrollOffset - new Point(leftMarginsWidth, 0));
			if (position.HasValue)
				textArea.Caret.Position = position.Value;
		};

		//TextArea textArea = Editor.TextArea;
		//textArea.LeftMargins.Add(new FoldingMargin());

		IReadOnlyCollection<PluginPartsRegistrator.EditorDecoratorRegistration> decorators = pluginPartsRegistrator.GetEditorDecorators();
		if (decorators.Count != 0)
		{
			EditorDecoratorRegistratorContext context = new EditorDecoratorRegistratorContext(Editor, _serviceProvider);
			foreach (PluginPartsRegistrator.EditorDecoratorRegistration registration in decorators)
				registration.Callback(context);
		}
	}

	internal void NavigateTo(int offset)
	{
		TextEditor editor = Editor;
		editor.CaretOffset = offset;
		int line = editor.Document.GetLineByOffset(offset).LineNumber;
		editor.ScrollToLine(line);
		editor.TextArea.Focus();
	}

	internal void NavigateTo(int line, int? column)
	{
		TextEditor editor = Editor;
		editor.CaretOffset = editor.Document.GetOffset(line, column ?? 1);
		editor.ScrollToLine(line);
		editor.TextArea.Focus();
	}

	sealed record EditorDecoratorRegistratorContext(TextEditor Editor, IServiceProvider ServiceProvider) : IEditorDecoratorRegistratorContext;
}

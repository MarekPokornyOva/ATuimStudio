using Avalonia.Controls;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using AvaloniaEdit.Editing;
using ATuimStudio.Services;
using System.ComponentModel;
using ATuimStudio.Extensibility;
using AvaloniaEdit;
using Avalonia;
using ATuimStudio.Extensions.Core;
using ATuimStudio.ViewModels;
using Dock.Model.Core;

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

	readonly static double _defaultFontSize = 14/*TextElement.FontSizeProperty.GetDefaultValue(typeof(TextEditor))*/;

	readonly IUserOptionsManager _userOptionsManager;
	readonly DockFactory _dockFactory;
	public DocumentView(IPluginPartsRegistrator pluginPartsRegistrator, IServiceProvider serviceProvider, IUserOptionsManager userOptionsManager, DockFactory dockFactory) : this()
	{
		_serviceProvider = serviceProvider;
		_userOptionsManager = userOptionsManager;
		_dockFactory = dockFactory;

		if (userOptionsManager.TryGetDouble(UserOptionsCodes.DocumentEditorZoom, out double zoom))
			NotifyZoom(zoom);

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

	#region Zoom
	readonly static char[] _zoomTrimChars = [' ', '%'];
	internal void ZoomSelected(object? sender, SelectionChangedEventArgs e)
	{
		e.Handled = true;

		if (sender is ComboBox zoomCb
			&& zoomCb.SelectedItem is ComboBoxItem zoomCbi
			&& zoomCbi.Content is string zoomStr)
			SetZoom(zoomStr);
	}

	public void ZoomLosingFocus(object? sender, Avalonia.Input.FocusChangingEventArgs e)
	{
		e.Handled = true;
		if (sender is ComboBox { Text: { } } zoomCb)
			SetZoom(zoomCb.Text);
	}

	void SetZoom(string zoomStr)
	{
		if (Editor != null && double.TryParse(zoomStr.TrimEnd(_zoomTrimChars), out double zoom))
		{
			ApplyZoom(zoom);
			_userOptionsManager.SetValue(UserOptionsCodes.DocumentEditorZoom, zoom);

			//Notify other documents to share the zoom value
			IList<IDockable>? dockables = _dockFactory.DocumentDock.VisibleDockables;
			if (dockables != null)
				foreach (DocumentView documentView in dockables.OfType<IGeneralDocumentDockBase>().Select(static x => x.TryGetView(out Control? view) ? view : default).Where(static x => x != default).Where(x => x != this).OfType<DocumentView>())
					documentView.NotifyZoom(zoom);
		}
	}

	void ApplyZoom(double zoom)
	{
		Editor.FontSize = _defaultFontSize * zoom / 100.0;
	}

	void NotifyZoom(double zoom)
	{
		ApplyZoom(zoom);
		ZoomSelector.Text = $"{zoom} %";
	}
	#endregion Zoom

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

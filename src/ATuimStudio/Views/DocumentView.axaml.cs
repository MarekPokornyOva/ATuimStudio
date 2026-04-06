using Avalonia.Controls;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
//using AvaloniaEdit.Editing;
using ATuimStudio.Services;
using System.ComponentModel;
using ATuimStudio.Extensibility;
using AvaloniaEdit;

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

	sealed record EditorDecoratorRegistratorContext(TextEditor Editor, IServiceProvider ServiceProvider) : IEditorDecoratorRegistratorContext;
}

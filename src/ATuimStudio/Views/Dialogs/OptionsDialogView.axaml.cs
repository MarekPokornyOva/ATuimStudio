using ATuimStudio.ViewModels;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace ATuimStudio.Views;

public partial class OptionsDialogView : UserControl
{
	public OptionsDialogView()
	{
		InitializeComponent();
	}
}

//https://avaloniaui.github.io/Avalonia.Samples/src/Avalonia.Samples/DataTemplates/IDataTemplateSample/
public sealed class OptionValueTemplateSelector : IDataTemplate
{
	[Content]
	public Dictionary<string, IDataTemplate> AvailableTemplates { get; } = [];

	public string DefaultKey { get; set; } = default!;

	public bool Match(object? data)
		=> data is OptionsDialogViewModel.OptionItem;

	public Control Build(object? param)
	{
		if (param is not OptionsDialogViewModel.OptionItem optionItem)
			throw new ArgumentNullException(nameof(param));
		return (AvailableTemplates.TryGetValue(optionItem.Value.GetType().Name, out IDataTemplate? template)
			? template
			: AvailableTemplates[DefaultKey])
			.Build(param)!;
	}
}

using ATuimStudio.Extensibility;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio.Extensions.TextEditCompletion
{
	public sealed class Extension : ATuimStudio.Extensibility.UiExtension
	{
		public override void RegisterEditorDecorator(IEditorDecoratorRegistrator editorDecoratorRegistrator)
		{
			editorDecoratorRegistrator.Register(static context =>
			{
				ITextEditCompletionProvider textEditCompletionProvider = context.ServiceProvider.GetRequiredService<ITextEditCompletionProvider>();

				TextArea textArea = context.Editor.TextArea;
#pragma warning disable CA1806
				new CompletionHelper(textArea, textEditCompletionProvider);
#pragma warning restore CA1806
			});
		}

		public override void RegisterServices(IServiceCollection services)
			=> TextEditCompletionServiceCollectionExtensions.AddTextEditCompletion(services);
	}

	class CompletionHelper
	{
		readonly TextArea _textArea;
		readonly ITextEditCompletionProvider _textEditCompletionProvider;
		internal CompletionHelper(TextArea textArea, ITextEditCompletionProvider textEditCompletionProvider)
		{
			_textArea = textArea;
			_textEditCompletionProvider = textEditCompletionProvider;
			textArea.TextEntered += TextArea_TextEntered;
		}

		//https://deepwiki.com/avaloniaui/avaloniaedit/1.2-getting-started
		//https://github.com/avaloniaui/avaloniaedit/blob/4290c429/src/AvaloniaEdit.Demo/MainWindow.xaml.cs#L285-L336
		//https://github.com/AvaloniaUI/AvaloniaEdit/issues/460
		void TextArea_TextEntered(object? sender, Avalonia.Input.TextInputEventArgs e)
		{
			if (e.Text == ".")
			{
				CompletionWindow completionWindow = new CompletionWindow(_textArea);
				//completionWindow.Closed += (o, args) => 

				Task<IReadOnlyCollection<ITextEditCompletionItem>> task = _textEditCompletionProvider.GetCompletions(_textArea.Document.FileName, _textArea.Caret.Offset, CancellationToken.None);
				task.GetAwaiter().OnCompleted(() =>
				{
					IReadOnlyCollection<ITextEditCompletionItem> completionData = task.Result;
					if (completionData.Count != 0)
					{
						completionWindow.CompletionList.CompletionData.AddRange(completionData.Select<ITextEditCompletionItem, ICompletionData>(static x => new CompletionItem(x)));
						completionWindow.Show();
					}
				});
			}
		}

		class CompletionItem : ICompletionData
		{
			readonly ITextEditCompletionItem _item;
			string? _description;
			internal CompletionItem(ITextEditCompletionItem item)
			{
				_item = item;
			}

			public IImage Image => null!;
			public string Text => _item.Text;
			public object Content => _item.Label;
			public object Description => _description ??= string.Join(Environment.NewLine, _item.CodeItems.Select(static x => x.Text));
			public double Priority => _item.Priority;

			public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
			{
				textArea.Document.Replace(completionSegment, _item.Text);
			}
		}
	}
}

using ATuimStudio.Extensibility;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Text;

namespace ATuimStudio.Extensions.TextEditCompletion
{
	public sealed class Extension : ATuimStudio.Extensibility.UiExtension
	{
		public override void RegisterEditorDecorator(IEditorDecoratorRegistrator editorDecoratorRegistrator)
		{
			editorDecoratorRegistrator.Register(static context =>
#pragma warning disable CA1806
				new CompletionHelper(context.Editor.TextArea, context.ServiceProvider.GetRequiredService<ITextEditCompletionProvider>(), context.ServiceProvider.GetRequiredService<ICodeInsightProvider>())
#pragma warning restore CA1806
			);
		}

		public override void RegisterServices(IServiceCollection services)
			=> TextEditCompletionServiceCollectionExtensions.AddTextEditCompletion(services);
	}

	class CompletionHelper
	{
		readonly TextArea _textArea;
		readonly ITextEditCompletionProvider _textEditCompletionProvider;
		readonly ICodeInsightProvider _codeInsightProvider;
		internal CompletionHelper(TextArea textArea, ITextEditCompletionProvider textEditCompletionProvider, ICodeInsightProvider codeInsightProvider)
		{
			_textArea = textArea;
			_textEditCompletionProvider = textEditCompletionProvider;
			_codeInsightProvider = codeInsightProvider;

			textArea.TextEntered += TextArea_TextEntered;
			_textArea.DefaultInputHandler.CommandBindings.Add(new RoutedCommandBinding(new RoutedCommand("CtrlSpaceCompletion", new KeyGesture(Key.Space, KeyModifiers.Control)), OnCodeCompletion));
			_textArea.DefaultInputHandler.CommandBindings.Add(new RoutedCommandBinding(new RoutedCommand("CtrlShiftSpaceInsight", new KeyGesture(Key.Space, KeyModifiers.Control | KeyModifiers.Shift)), OnCodeInsight));
		}

		//https://deepwiki.com/avaloniaui/avaloniaedit/1.2-getting-started
		//https://github.com/avaloniaui/avaloniaedit/blob/4290c429/src/AvaloniaEdit.Demo/MainWindow.xaml.cs#L285-L336
		//https://github.com/AvaloniaUI/AvaloniaEdit/issues/460
		void TextArea_TextEntered(object? sender, Avalonia.Input.TextInputEventArgs e)
		{
			if (e.Text == ".")
				HandleCompletion();
		}

		void HandleCompletion()
		{
			Task<ITextEditCompletionResult> task = _textEditCompletionProvider.GetCompletions(_textArea.Document.FileName, _textArea.Caret.Offset, CancellationToken.None);
			task.GetAwaiter().OnCompleted(() =>
			{
				ITextEditCompletionResult completionResult = task.Result;
				IReadOnlyCollection<ITextEditCompletionItem> completionItems = completionResult.Items;
				if (completionItems.Count != 0)
				{
					CompletionWindow completionWindow = new CompletionWindow(_textArea);
					//completionWindow.Closed += (o, args) => 

					TextEditCompletionIdentifier? identifier = completionResult.Identifier;
					(int identStart, int identLen) = identifier.HasValue ? (identifier.Value.Start, identifier.Value.End - identifier.Value.Start) : (0, 0);
					completionWindow.CompletionList.CompletionData.AddRange(completionItems.Select(x => new CompletionItem(x, identStart, identLen)));
					//completionWindow.CompletionList.SelectedItem = completionWindow.CompletionList.CompletionData[0];
					completionWindow.Show();
				}
			});
		}

		sealed class CompletionItem : ICompletionData
		{
			static readonly StringBuilder _sb = new StringBuilder();
			static readonly Lock _sbLock = new Lock();
			readonly ITextEditCompletionItem _item;
			readonly int _identStart;
			readonly int _identLen;
			string? _description;
			internal CompletionItem(ITextEditCompletionItem item, int identStart, int identLength)
			{
				_item = item;
				_identStart = identStart;
				_identLen = identLength;
			}

			public IImage Image => null!;
			public string Text => _item.Text;
			public object Content => _item.Label;
			public object Description
			{
				get
				{
					if (_description == null)
						lock (_sbLock)
						{
							StringBuilder sb = _sb;
							sb.Clear();
							bool isFirst = true;
							foreach (ICodeEditCompletionItem item in _item.CodeItems)
							{
								if (isFirst)
									isFirst = false;
								else
									sb.AppendLine();
								sb.AppendLine(item.Text).Append(item.Description);
							}
							_description = sb.ToString();
						}
					return _description;
				}
			}
			public double Priority => _item.Priority;

			public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
			{
				if (_identLen != 0)
					completionSegment = new AnchorSegment(textArea.Document, _identStart, _identLen);
				textArea.Document.Replace(completionSegment, _item.Text);
			}
		}

		void OnCodeCompletion(object? sender, ExecutedRoutedEventArgs e)
		{
			e.Handled = true;
			HandleCompletion();
		}

		static readonly OverLoadProvider _overLoadProvider = new OverLoadProvider();
		void OnCodeInsight(object? sender, ExecutedRoutedEventArgs e)
		{
			e.Handled = true;

			Task<ICodeInsightResult> task = _codeInsightProvider.Get(_textArea.Document.FileName, _textArea.Caret.Offset, CancellationToken.None);
			task.GetAwaiter().OnCompleted(() =>
			{
				ICodeInsightResult result = task.Result;
				if (result.Overloads.Count != 0)
				{
					OverloadInsightWindow insightWindow = new OverloadInsightWindow(_textArea);
					//insightWindow.Closed += (o, args) => 
				
					_overLoadProvider.Setup(result);
					insightWindow.Provider = _overLoadProvider;
					insightWindow.Show();
				}
			});
		}

		sealed class OverLoadProvider : IOverloadProvider
		{
			ICodeInsightMethodOverload[] _overloads = default!;
			readonly StringBuilder _sb = new StringBuilder();
			internal void Setup(ICodeInsightResult overloadsResult)
			{
				ICodeInsightMethodOverload[] overloads = [.. overloadsResult.Overloads];
				_overloads = overloads;
				Count = overloads.Length;
				SelectedIndex = overloadsResult.BestCandidate ?? 0;
			}

			void UpdateFields(int selectedIndex)
			{
				ICodeInsightMethodOverload overload = _overloads[selectedIndex];

				CurrentIndexText = $"{selectedIndex + 1}/{Count}";
				CurrentHeader = overload.Signature;

				StringBuilder sb = _sb;
				sb.Clear();
				if (!string.IsNullOrEmpty(overload.Summary))
					sb.AppendLine(overload.Summary);
				if (!string.IsNullOrEmpty(overload.ReturnDescription))
					sb.AppendLine(overload.ReturnDescription);

				if (overload.Parameters.Count != 0)
				{
					sb.AppendLine().AppendLine("Parameters:");
					foreach (ICodeInsightMethodParameter parm in overload.Parameters)
						sb.Append(parm.Name).Append(": ").AppendLine(parm.Description);
				}

				if (overload.Exceptions.Count != 0)
				{
					sb.AppendLine().AppendLine("Exceptions:");
					foreach (ICodeInsightMethodException exc in overload.Exceptions)
						sb.Append(exc.ExceptionType).Append(": ").AppendLine(exc.Description);
				}

				//trim ending new-line
				if (sb.Length != 0)
					sb.Remove(sb.Length - 2, 2);

				CurrentContent = sb.ToString();
			}

			public int SelectedIndex { get; set { field = value; UpdateFields(value); } }

			public int Count { get; private set; }

			public string CurrentIndexText { get; set { field = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentIndexText))); } } = default!;

			public object CurrentHeader { get; set { field = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentHeader))); } } = default!;

			public object CurrentContent { get; set { field = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentContent))); } } = default!;

			public event PropertyChangedEventHandler? PropertyChanged;
		}
	}
}

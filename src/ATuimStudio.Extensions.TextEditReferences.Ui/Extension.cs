using ATuimStudio.Extensibility;
using ATuimStudio.Extensions.Core;
using ATuimStudio.Extensions.Core.Ui;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio.Extensions.TextEditReferences
{
	public sealed class Extension : ATuimStudio.Extensibility.UiExtension
	{
		internal const string IdReferences = "References";
		internal readonly static Guid TypeReferences = new Guid(0x46ef0ea4, 0x5ae6, 0x4d81, 0xa4, 0xee, 0xc3, 0x41, 0x48, 0x6e, 0x29, 0xbc);

		public override void RegisterEditorDecorator(IEditorDecoratorRegistrator editorDecoratorRegistrator)
		{
			editorDecoratorRegistrator.Register(static context =>
#pragma warning disable CA1806
				new FunctionsHelper(context.Editor.TextArea, context.ServiceProvider.GetRequiredService<IUiDocumentService>(), context.ServiceProvider.GetRequiredService<IUiWindowService>(), context.ServiceProvider.GetRequiredService<IReferencesFinder>(), context.ServiceProvider.GetRequiredService<IQuickInfoProvider>(), context.ServiceProvider.GetRequiredService<IPub<IAllReferencesResult>>(), context.ServiceProvider.GetRequiredService<IPub<IFindImplementationsResult>>())
#pragma warning restore CA1806
			);
		}

		public override void RegisterLayoutWindow(ILayoutWindowRegistrator layoutWindowRegistrator)
		{
			layoutWindowRegistrator.RegisterPaneFactory(TypeReferences,
				static sp => ActivatorUtilities.CreateInstance<AllReferencesViewModel>(sp),
				static sp => new AllReferencesView());
		}

		public override void RegisterServices(IServiceCollection services)
			=> TextEditReferencesServiceCollectionExtensions.AddTextEditReferences(services);
	}

	class FunctionsHelper
	{
		readonly TextArea _textArea;
		readonly IUiDocumentService _documentService;
		readonly IUiWindowService _uiWindowService;
		readonly IReferencesFinder _referencesFinder;
		readonly IPub<IAllReferencesResult> _pubReferences;
		readonly IPub<IFindImplementationsResult> _pubImplementations;
		internal FunctionsHelper(TextArea textArea, IUiDocumentService uiDocumentService, IUiWindowService uiWindowService, IReferencesFinder referencesFinder, IQuickInfoProvider quickInfoProvider, IPub<IAllReferencesResult> pubReferences, IPub<IFindImplementationsResult> pubImplementations)
		{
			_textArea = textArea;
			_documentService = uiDocumentService;
			_uiWindowService = uiWindowService;
			_referencesFinder = referencesFinder;
			_pubReferences = pubReferences;
			_pubImplementations = pubImplementations;

			ContextMenu contextMenu = textArea.ContextMenu ??= new ContextMenu { Cursor = Cursor.Default };
			KeyGesture goToDefinitionKeyGesture, goToImplementationKeyGesture;
			contextMenu.Items.Add(new MenuItem
			{
				Header = "Find all references",
				Command = new RelayCommand(FindAllReferences)
			});
			contextMenu.Items.Add(new MenuItem
			{
				Header = "Go to definition",
				Command = new RelayCommand(GoToDefinition),
				InputGesture = goToDefinitionKeyGesture = new KeyGesture(Key.F12)
			});
			contextMenu.Items.Add(new MenuItem
			{
				Header = "Go to implementation",
				Command = new RelayCommand(GoToImplementation),
				InputGesture = goToImplementationKeyGesture = new KeyGesture(Key.F12, KeyModifiers.Control)
			});

			_textArea.DefaultInputHandler.CommandBindings.Add(new RoutedCommandBinding(new RoutedCommand("F12Definition", goToDefinitionKeyGesture), (_, _) => GoToDefinition()));
			_textArea.DefaultInputHandler.CommandBindings.Add(new RoutedCommandBinding(new RoutedCommand("CtrlF12Implementation", goToImplementationKeyGesture), (_, _) => GoToImplementation()));

			#region QuickInfo
			EditorHoverDetector hoverDetector = new EditorHoverDetector(textArea, 500, 10);
			hoverDetector.OnHoverDetected += (sender, position) =>
			{
				// Get the word or token at this position
				TextDocument document = textArea.Document;
				quickInfoProvider.GetAsync(document.FileName, document.GetOffset(position.Line, position.Column), CancellationToken.None)
					.ContinueWith(task =>
					{
						IQuickInfoResult? result = task.Result;
							Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
							{
								if (result == null)
									ToolTip.SetTip(textArea, null);
								else
								{
									// Display tooltip
									ToolTip.SetTip(textArea, string.Join(Environment.NewLine, result.Sections));
									ToolTip.SetIsOpen(textArea, true);
								}
							});
					});
			};
			hoverDetector.OnHoverEnded += (sender, e) =>
			{
				// Close tooltip
				ToolTip.SetTip(textArea, null);
			};
			_textArea.PointerWheelChanged += (s, e) => ToolTip.SetTip(textArea, null);
			#endregion QuickInfo
		}

		void FindAllReferences()
		{
			Task<IAllReferencesResult> task = _referencesFinder.FindAllReferencesAsync(_textArea.Document.FileName, _textArea.Caret.Offset, CancellationToken.None);
			task.GetAwaiter().OnCompleted(() =>
			{
				_uiWindowService.OpenPane(Extension.TypeReferences, WellKnownLayoutConstants.IdBasicInfo, Extension.IdReferences, "References");
				_pubReferences.Raise(task.Result);
			});
		}

		void GoToDefinition()
		{
			Task<IFindDefinitionResult> task = _referencesFinder.FindDefinitionAsync(_textArea.Document.FileName, _textArea.Caret.Offset, CancellationToken.None);
			task.GetAwaiter().OnCompleted(() =>
			{
				ReferenceItem definition = task.Result.Definition;
				if (definition == default)
					return;
				_documentService.SetActiveDocument(definition.FilePath, definition.Position);
			});
		}

		void GoToImplementation()
		{
			Task<IFindImplementationsResult> task = _referencesFinder.FindImplementationsAsync(_textArea.Document.FileName, _textArea.Caret.Offset, CancellationToken.None);
			task.GetAwaiter().OnCompleted(() =>
			{
				IReadOnlyCollection<ReferenceItem> implementations = task.Result.Implementations;
				switch (implementations.Count)
				{
					case 0:
						break;
					case 1:
						ReferenceItem impl = implementations.First();
						_documentService.SetActiveDocument(impl.FilePath, impl.Position);
						break;
					default:
						_uiWindowService.OpenPane(Extension.TypeReferences, WellKnownLayoutConstants.IdBasicInfo, Extension.IdReferences, "Implementations");
						_pubImplementations.Raise(task.Result);
						break;
				}
			});
		}
	}
}

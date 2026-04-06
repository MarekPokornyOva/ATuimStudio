using ATuimStudio.Extensibility;
using ATuimStudio.Extensions.Build;
using ATuimStudio.Extensions.Core;
using ATuimStudio.Extensions.Core.Ui;
using ATuimStudio.UiComponents;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Platform;
using Avalonia.Styling;
using AvaloniaEdit.Editing;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio.Extensions.Debug
{
	public sealed class Extension : ATuimStudio.Extensibility.UiExtension
	{
		const string StartCommandCode = "DebugStart";
		const string ContinueCommandCode = "DebugContinue";
		const string StepInCommandCode = "DebugStepIn";
		const string StepOverCommandCode = "DebugStepOver";
		const string StepOutCommandCode = "DebugStepOut";
		const string StopCommandCode = "DebugStop";
		const string OutputType = "Build";
		public override void RegisterCommand(ICommandRegistrator commandRegistrator)
		{
			IDebuggerService debuggerService = commandRegistrator.ServiceProvider.GetRequiredService<IDebuggerService>();
			IDebuggerProvider debuggerProvider = commandRegistrator.ServiceProvider.GetRequiredService<IDebuggerProvider>();
			IBreakpointManager breakpointManager = commandRegistrator.ServiceProvider.GetRequiredService<IBreakpointManager>();
			IBuildService buildService = commandRegistrator.ServiceProvider.GetRequiredService<IBuildService>();
			IOutputWriter outputWriter = commandRegistrator.ServiceProvider.GetRequiredService<IOutputWriter>();
			bool debuggerIsStopped = true;

			bool CanStart()
				=> debuggerProvider.Current == null;
			bool CanContinue()
				=> debuggerProvider.Current != null && debuggerIsStopped;
			bool CanStop()
				=> debuggerProvider.Current != null;

			commandRegistrator.Register(StartCommandCode, new AsyncRelayCommand(async (cancellationToken) =>
			{
				async Task<string?> Build(CancellationToken cancellationToken)
				{
					IServiceProvider sp = commandRegistrator.ServiceProvider;
					IOutputWriter outputWriter = sp.GetRequiredService<IOutputWriter>();
					IUiDocumentService uiDocumentService = sp.GetRequiredService<IUiDocumentService>();

					await uiDocumentService.SaveAllOpenedDocuments(cancellationToken);

					IProjectInfo? proj = uiDocumentService.GetActiveDocumentProject();
					if (proj == null)
					{
						outputWriter.Log(OutputType, OutputWriterSeverity.Info, $"No project selected");
						return null;
					}
					IBuildResult result = await sp.GetRequiredService<IBuildService>()
						.BuildProjectAsync(proj.Name, CancellationToken.None);
					if (!result.Success)
						outputWriter.Log(OutputType, OutputWriterSeverity.Error, "Build failed. Try build manually for more detail.");
					return result.OutputPath;
				}

				string? outputPath = await Build(cancellationToken);
				if (outputPath == null)
					return;

				IDebugger debugger = await debuggerService.CreateDebuggerAsync(outputPath, null, null, cancellationToken);

				void OnStarted(object? sender, BreakpointEventArgs e)
				{
					debugger.OnStarted -= OnStarted;

					foreach (Breakpoint bp in breakpointManager.Breakpoints)
						debugger.ToggleBreakpoint(bp.Filepath, new SourcePosition(bp.Range.Start.Line, bp.Range.Start.Column));
				}
				void DebugTerminated(object? sender, TerminatedEventArgs e)
				{
					debuggerIsStopped = true;
					IDebugger debugger = (IDebugger)sender!;
					debugger.OnBreakpoint -= DebugBreakpoint;
					debugger.OnContinued -= DebugContinued;
					debugger.OnTerminated -= DebugTerminated;
				}

				void DebugBreakpoint(object? sender, BreakpointEventArgs e)
				{
					debuggerIsStopped = true;
				}

				void DebugContinued(object? sender, BreakpointEventArgs e)
				{
					debuggerIsStopped = false;
				}

				debugger.OnStarted += OnStarted;
				debugger.OnBreakpoint += DebugBreakpoint;
				debugger.OnContinued += DebugContinued;
				debugger.OnTerminated += DebugTerminated;
				debugger.Start();
			}, CanStart),
				() => AssetLoader.Open(new Uri("avares://ATuimStudio.Extensions.Debug.Ui/Assets/DebugStart.png"))
				);

			commandRegistrator.Register(ContinueCommandCode, new RelayCommand(() =>
			{
				debuggerProvider.Current?.Continue();
			}, CanContinue),
				() => AssetLoader.Open(new Uri("avares://ATuimStudio.Extensions.Debug.Ui/Assets/DebugStart.png"))
				);

			commandRegistrator.Register(StopCommandCode, new RelayCommand(() =>
			{
				debuggerProvider.Current?.Terminate();
			}, CanStop),
				() => AssetLoader.Open(new Uri("avares://ATuimStudio.Extensions.Debug.Ui/Assets/DebugStop.png"))
				);

			commandRegistrator.Register(StepInCommandCode, new RelayCommand(() =>
			{
				debuggerProvider.Current?.StepIn();
			}, CanContinue),
				() => AssetLoader.Open(new Uri("avares://ATuimStudio.Extensions.Debug.Ui/Assets/DebugStepIn.png"))
				);

			commandRegistrator.Register(StepOverCommandCode, new RelayCommand(() =>
			{
				debuggerProvider.Current?.StepOver();
			}, CanContinue),
				() => AssetLoader.Open(new Uri("avares://ATuimStudio.Extensions.Debug.Ui/Assets/DebugStepOver.png"))
				);

			commandRegistrator.Register(StepOutCommandCode, new RelayCommand(() =>
			{
				debuggerProvider.Current?.StepOut();
			}, CanContinue),
				() => AssetLoader.Open(new Uri("avares://ATuimStudio.Extensions.Debug.Ui/Assets/DebugStepOut.png"))
				);
		}

		public override void RegisterMenu(IMenuRegistrator menuRegistrator)
		{
			menuRegistrator.Register(["Debug", "Start"], StartCommandCode, new KeyGesture(Key.F5));
			menuRegistrator.Register(["Debug", "Continue"], ContinueCommandCode, new KeyGesture(Key.F5));
			menuRegistrator.Register(["Debug", "Stop"], StopCommandCode, new KeyGesture(Key.F5, KeyModifiers.Shift));
			menuRegistrator.Register(["Debug", "Step In"], StepInCommandCode, new KeyGesture(Key.F11));
			menuRegistrator.Register(["Debug", "Step Over"], StepOverCommandCode, new KeyGesture(Key.F10));
			menuRegistrator.Register(["Debug", "Step Out"], StepOutCommandCode, new KeyGesture(Key.F11, KeyModifiers.Shift));
		}

		public override void RegisterLayoutWindow(ILayoutWindowRegistrator layoutWindowRegistrator)
		{
			layoutWindowRegistrator.Register(UiLayoutId.BelowDocuments, context => context.CreateViewModel<DebugCallStackViewModel>("DebugCallStack", "Call Stack"));
			layoutWindowRegistrator.Register(UiLayoutId.BelowDocuments, context => context.CreateViewModel<DebugLocalsViewModel>("DebugLocals", "Debug Locals"));
		}

		public override void RegisterEditorDecorator(IEditorDecoratorRegistrator editorDecoratorRegistrator)
		{
			editorDecoratorRegistrator.Register(context =>
			{
				IBreakpointManager breakpointManager = context.ServiceProvider.GetRequiredService<IBreakpointManager>();
				IStackTraceProvider stackTraceProvider = context.ServiceProvider.GetRequiredService<IStackTraceProvider>();

				context.Editor.KeyBindings.Add(new KeyBinding
				{
					Gesture = new KeyGesture(Key.F9),
					Command = new RelayCommand(() =>
					{
						TextArea textArea = context.Editor.TextArea;
						Caret caret = textArea.Caret;
						breakpointManager.ToggleBreakpoint(textArea.Document.FileName, caret.Line, caret.Column);
					})
				});

				TextArea textArea = context.Editor.TextArea;
				textArea.LeftMargins.Insert(0, new BreakpointMargin(breakpointManager));
				//https://deepwiki.com/avaloniaui/avaloniaedit/5-extending-avaloniaedit
				textArea.TextView.LineTransformers.Add(new BreakpointLineTransformer(textArea.TextView, breakpointManager, stackTraceProvider));
			});
		}

		public override void RegisterServices(IServiceCollection services)
			=> DebugServiceCollectionExtensions.AddDebug(services);

		public override void RegisterStyles(Action<IStyle> styleAppender)
		{
			Uri uri = new Uri("avares://Avalonia.Controls.TreeDataGrid/Themes/Fluent.axaml");
			styleAppender(new StyleInclude(uri) { Source = uri });
		}
	}
}

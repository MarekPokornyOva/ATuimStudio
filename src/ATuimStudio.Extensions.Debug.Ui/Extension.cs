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
		const string IdDebugCallStack = "DebugCallStack";
		const string IdDebugLocals = "DebugLocals";
		const string IdDebugBreakpoints = "Breakpoints";
		readonly static Guid TypeDebugCallStack = new Guid(0x511df1f7, 0x2ce1, 0x41d3, 0xaa, 0x56, 0xff, 0xaf, 0x57, 0xbe, 0x49, 0x72);
		readonly static Guid TypeDebugLocals = new Guid(0x46b21adf, 0xbb91, 0x4ca4, 0x96, 0x81, 0x64, 0x32, 0x7f, 0x2b, 0x69, 0xd0);
		readonly static Guid TypeDebugBreakpoints = new Guid(0xdc9a22f3, 0xe8dd, 0x44d4, 0xb6, 0xb1, 0x80, 0xe2, 0x40, 0x94, 0x91, 0x41);

		public override void RegisterCommand(ICommandRegistrator commandRegistrator)
		{
			IDebuggerService debuggerService = commandRegistrator.ServiceProvider.GetRequiredService<IDebuggerService>();
			IDebuggerProvider debuggerProvider = commandRegistrator.ServiceProvider.GetRequiredService<IDebuggerProvider>();
			IBreakpointManager breakpointManager = commandRegistrator.ServiceProvider.GetRequiredService<IBreakpointManager>();
			IBuildService buildService = commandRegistrator.ServiceProvider.GetRequiredService<IBuildService>();
			IOutputWriter outputWriter = commandRegistrator.ServiceProvider.GetRequiredService<IOutputWriter>();
			ILayoutManager layoutManager = commandRegistrator.ServiceProvider.GetRequiredService<ILayoutManager>();
			bool debuggerIsStopped = true;

			bool CanStart()
				=> debuggerProvider.Current == null;
			bool CanContinue()
				=> debuggerProvider.Current != null && debuggerIsStopped;
			bool CanStop()
				=> debuggerProvider.Current != null;

			List<IRelayCommand> allCommands = new List<IRelayCommand>(6);
			T AddCommand<T>(T command) where T : IRelayCommand
			{
				allCommands.Add(command);
				return command;
			}

			void RefreshCommands()
			{
				Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
				{
					foreach (IRelayCommand command in allCommands)
						command.NotifyCanExecuteChanged();
				});
			}

			commandRegistrator.Register(StartCommandCode, AddCommand(new AsyncRelayCommand(async (cancellationToken) =>
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

						layoutManager.SwitchLayout("Debug");

						foreach (Breakpoint bp in breakpointManager.Breakpoints)
							debugger.ToggleBreakpoint(bp.Filepath, new SourcePosition(bp.Range.Start.Line, bp.Range.Start.Column));

						RefreshCommands();
					}
					void DebugTerminated(object? sender, TerminatedEventArgs e)
					{
						debuggerIsStopped = true;
						IDebugger debugger = (IDebugger)sender!;
						debugger.OnBreakpoint -= DebugBreakpoint;
						debugger.OnContinued -= DebugContinued;
						debugger.OnTerminated -= DebugTerminated;
						RefreshCommands();

						layoutManager.SwitchLayout(WellKnownLayoutConstants.LayoutBasic);
					}

					void DebugBreakpoint(object? sender, BreakpointEventArgs e)
					{
						debuggerIsStopped = true;
						RefreshCommands();
					}

					void DebugContinued(object? sender, BreakpointEventArgs e)
					{
						debuggerIsStopped = false;
						RefreshCommands();
					}

					debugger.OnStarted += OnStarted;
					debugger.OnBreakpoint += DebugBreakpoint;
					debugger.OnContinued += DebugContinued;
					debugger.OnTerminated += DebugTerminated;
					debugger.Start();
				}, CanStart)),
				() => AssetLoader.Open(new Uri("avares://ATuimStudio.Extensions.Debug.Ui/Assets/DebugStart.png"))
			);

			commandRegistrator.Register(ContinueCommandCode, AddCommand(new RelayCommand(() =>
				{
					debuggerProvider.Current?.Continue();
				}, CanContinue)),
				() => AssetLoader.Open(new Uri("avares://ATuimStudio.Extensions.Debug.Ui/Assets/DebugStart.png"))
				);

			commandRegistrator.Register(StopCommandCode, AddCommand(new RelayCommand(() =>
				{
					debuggerProvider.Current?.Terminate();
				}, CanStop)),
				() => AssetLoader.Open(new Uri("avares://ATuimStudio.Extensions.Debug.Ui/Assets/DebugStop.png"))
				);

			commandRegistrator.Register(StepInCommandCode, AddCommand(new RelayCommand(() =>
				{
					debuggerProvider.Current?.StepIn();
				}, CanContinue)),
				() => AssetLoader.Open(new Uri("avares://ATuimStudio.Extensions.Debug.Ui/Assets/DebugStepIn.png"))
				);

			commandRegistrator.Register(StepOverCommandCode, AddCommand(new RelayCommand(() =>
				{
					debuggerProvider.Current?.StepOver();
				}, CanContinue)),
				() => AssetLoader.Open(new Uri("avares://ATuimStudio.Extensions.Debug.Ui/Assets/DebugStepOver.png"))
				);

			commandRegistrator.Register(StepOutCommandCode, AddCommand(new RelayCommand(() =>
				{
					debuggerProvider.Current?.StepOut();
				}, CanContinue)),
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
			layoutWindowRegistrator.RegisterPaneFactory(TypeDebugCallStack,
				static sp => ActivatorUtilities.CreateInstance<DebugCallStackViewModel>(sp),
				static sp => new DebugCallStackView());
			layoutWindowRegistrator.RegisterPaneFactory(TypeDebugLocals,
				static sp => ActivatorUtilities.CreateInstance<DebugLocalsViewModel>(sp),
				static sp => new DebugLocalsView());
			layoutWindowRegistrator.RegisterPaneFactory(TypeDebugBreakpoints,
				static sp => ActivatorUtilities.CreateInstance<DebugBreakpointsViewModel>(sp),
				static sp => new DebugBreakpointsView());

			layoutWindowRegistrator.RegisterParts("Debug", static ctx =>
			{
				ctx.Layout.FindPanesContainer(WellKnownLayoutConstants.IdBasicInfo)
					.AddPane(IdDebugCallStack, "Call Stack", TypeDebugCallStack);
				ctx.Layout.FindPanesContainer(WellKnownLayoutConstants.IdBasicInfo)
					.AddPane(IdDebugLocals, "Debug Locals", TypeDebugLocals);
				ctx.Layout.FindPanesContainer(WellKnownLayoutConstants.IdBasicInfo)
					.AddPane(IdDebugBreakpoints, "Breakpoints", TypeDebugBreakpoints);
			});
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

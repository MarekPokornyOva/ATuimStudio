using AvalonStudio.Debugging.DotNetCore;
using Microsoft.Extensions.DependencyInjection;
using Mono.Debugging.Client;
using OmniSharp.Extensions.DebugAdapter.Protocol.Events;
using OmniSharp.Extensions.DebugAdapter.Protocol.Models;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;
using OmniSharp.Extensions.DebugAdapter.Server;
using OmniSharp.Extensions.JsonRpc;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;

namespace ATuimStudio.Debugger.Dap.Adapter
{
	sealed class SessionContext
	{
		internal DebuggerSession DebuggerSession = default!;
		internal DebugAdapterServer DebugAdapterServer = default!;
		internal FrameReferenceManager FrameReferenceManager = new FrameReferenceManager();
		internal VariableReferenceManager VariableReferenceManager = new VariableReferenceManager();

		internal void ClearReferences()
		{
			FrameReferenceManager.Clear();
			VariableReferenceManager.Clear();
		}
	}

	internal class Program
	{
		internal static readonly TaskCompletionSource<object?> ExitSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);

		static async Task Main(string[] args)
		{
			Dictionary<string, string?> appArguments = TranslateAppArguments(args);
			int port = 0;
			string namedPipeName = "";
			if (!(
					appArguments.TryGetValue("InterfaceType", out string? interfaceTypeStr) && Enum.TryParse<InterfaceType>(interfaceTypeStr, true, out InterfaceType interfaceType)
					&& (
							interfaceType == InterfaceType.StdIo ||
							(interfaceType == InterfaceType.Tcp && appArguments.TryGetValue("Port", out string? portStr) && int.TryParse(portStr, out port)) ||
							(interfaceType == InterfaceType.NamedPipe && (appArguments.TryGetValue("NamedPipeName", out string? namedPipeNameTemp)) && (namedPipeName = namedPipeNameTemp!) != null)
						)
				))
			{
				Console.WriteLine(
					$"""
					ATuim debug adapter
					Usage: ATuimDbg <arguments>
					Arguments list:
					  --InterfaceType	The way client wants to communicate with adapter: {InterfaceType.StdIo},{InterfaceType.Tcp},{InterfaceType.NamedPipe}
					  --Port				Port for communication using tcp
					  --NoClose			Specify to do not close the terminal window
					Example: atuimdbg --interfacetype=stdio
								atuimdbg --interfacetype=tcp --port=8888
								atuimdbg --interfacetype=namedpipe --namedpipename=dbg
					""");
				return;
			}

			(Action<DebugAdapterServerOptions> debugAdapterServerInitializer, Action finalizer) =
				interfaceType switch
				{
					InterfaceType.StdIo => CreateStdIo(),
					InterfaceType.Tcp => await CreateTcpAsync(port),
					InterfaceType.NamedPipe => await CreateNamedPipeAsync(namedPipeName),
					_ => throw new InvalidOperationException()
				};

			DebuggerSession session = DotNetCoreDebugger.CreateSession();
			SessionContext sessionContext = new SessionContext
			{
				DebuggerSession = session,
			};

			DebugAdapterServer server = await DebugAdapterServer.From(options =>
			{
				options
					.WithServices(services =>
						services
							.AddSingleton<SessionContext>(sessionContext)
					)
					.WithHandler<LaunchHandler>()
					.WithHandler<DisconnectHandler>()
					.WithHandler<ConfigurationDoneHandler>()
					.WithHandler<ThreadsHandler>()
					.WithHandler<SetBreakpointsHandler>()
					.WithHandler<StackTraceHandler>()
					.WithHandler<VariablesHandler>()
					.WithHandler<EvaluateHandler>()
					.WithHandler<TerminateHandler>()
					.WithHandler<ContinueHandler>()
					.WithHandler<StepInHandler>()
					.WithHandler<StepOutHandler>()
					.WithHandler<StepOverHandler>()
					.OnInitialized((srv, request, response, token) =>
					{
						response.SupportsCancelRequest = false;
						response.SupportsConfigurationDoneRequest = true;
						response.SupportsInstructionBreakpoints = true;
						response.SupportsTerminateRequest = true;
						return Task.CompletedTask;
					});
				debugAdapterServerInitializer(options);
			});

			//session.TargetEvent += (s, e) => Console.WriteLine(e.Type);
			//session.TargetUnhandledException += (s, e) => Console.WriteLine(e.Type);
			session.TargetHitBreakpoint += (sender, e) => ReportBreakpoint(e, server);
			session.TargetStopped += (sender, e) => ReportBreakpoint(e, server);
			session.TargetExited += (sender, e) =>
			{
				((DebuggerSession)sender!).Exit();
				Program.ExitSignal.TrySetResult(null);
				sessionContext.DebugAdapterServer.SendTerminated(new TerminatedEvent());
			};

			server.GetRequiredService<SessionContext>().DebugAdapterServer = server;

			await ExitSignal.Task;
			server.Dispose();

			finalizer();

			if (appArguments.ContainsKey("NoClose"))
			{
				Console.WriteLine("Press any key to close the window.");
				Console.ReadKey(true);
			}
		}

		#region create initializers
		static (Action<DebugAdapterServerOptions> DebugAdapterServerInitializer, Action Finalizer) CreateStdIo()
			=> (
					static options => options
						.WithInput(Console.OpenStandardInput())
						.WithOutput(Console.OpenStandardOutput()),
					static () => { }
		);

		static async Task<(Action<DebugAdapterServerOptions> DebugAdapterServerInitializer, Action Finalizer)> CreateTcpAsync(int port)
		{
			TcpListener listener = new TcpListener(IPAddress.Any, port);
			listener.Start();
			TcpClient client = await listener.AcceptTcpClientAsync();
			NetworkStream stream = client.GetStream();

			return (
						options => options
							.WithInput(stream)
							.WithOutput(stream),
						() =>
						{
							stream.Dispose();
							client.Dispose();
						}
			);
		}

		static async Task<(Action<DebugAdapterServerOptions> DebugAdapterServerInitializer, Action Finalizer)> CreateNamedPipeAsync(string pipeName)
		{
			NamedPipeServerStream pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
			await pipe.WaitForConnectionAsync();
			return (
						options => options
							.WithInput(pipe)
							.WithOutput(pipe),
						pipe.Dispose
					);
		}

		static Dictionary<string, string?> TranslateAppArguments(string[] args)
		{
			Dictionary<string, string?> result = new Dictionary<string, string?>(args.Length, StringComparer.InvariantCultureIgnoreCase);

			foreach (string arg in args)
			{
				if (!arg.StartsWith("--"))
					continue;
				int pos = arg.IndexOf('=');
				if (pos == -1)
					result[arg] = null;
				else
					result[arg[2..pos]] = arg[(pos + 1)..];
			}

			return result;
		}
		#endregion create initializers

		static void ReportBreakpoint(TargetEventArgs e, IResponseRouter rr)
		{
			if (e.Backtrace != null)
				rr.SendStopped(new StoppedEvent { AllThreadsStopped = true, Reason = StoppedEventReason.InstructionBreakpoint, ThreadId = e.Thread.Id });
		}
	}

	internal class LaunchHandler : LaunchHandlerBase<LaunchRequestArguments>
	{
		readonly SessionContext _sessionContext;
		public LaunchHandler(SessionContext sessionContext/*, IResponseRouter rr*/)
		{
			_sessionContext = sessionContext;
			//_rr = rr;
		}

		public override Task<LaunchResponse> Handle(LaunchRequestArguments request, CancellationToken cancellationToken)
		{
			string program = (string)request.ExtensionData["program"];
			string[] args = [];// request.ExtensionData["args"].AsJsonStrings();
			string cwd = (string)request.ExtensionData["cwd"];
			//string[] envs = [];// request.ExtensionData["args"].AsJsonStrings();
			//bool stopOnEntry = (bool)request.ExtensionData["stopOnEntry"];

			//start
			DebuggerStartInfo startinfo = DotNetCoreDebugger.GetDebuggerStartInfo(program, null, null);
			_sessionContext.DebuggerSession.Run(startinfo, DotNetCoreDebugger.GetDebuggerSessionOptions());

			return Task.FromResult(new LaunchResponse());
		}
	}

	class ConfigurationDoneHandler : ConfigurationDoneHandlerBase
	{
		public override Task<ConfigurationDoneResponse> Handle(ConfigurationDoneArguments request, CancellationToken cancellationToken)
			=> Task.FromResult(new ConfigurationDoneResponse());
	}

	class ThreadsHandler : ThreadsHandlerBase
	{
		private readonly SessionContext _sessionContext;
		public ThreadsHandler(SessionContext sessionContext) => _sessionContext = sessionContext;

		public override Task<ThreadsResponse> Handle(ThreadsArguments request, CancellationToken cancellationToken)
			=> Task.FromResult(new ThreadsResponse
			{
				Threads = new Container<OmniSharp.Extensions.DebugAdapter.Protocol.Models.Thread>(
					[.. _sessionContext.DebuggerSession.GetProcesses().SelectMany(static x => x.GetThreads()).Select(static x =>
						new OmniSharp.Extensions.DebugAdapter.Protocol.Models.Thread { Id = x.Id, Name = x.Name })])
			});
	}

	class SetBreakpointsHandler : SetBreakpointsHandlerBase
	{
		private readonly SessionContext _sessionContext;
		public SetBreakpointsHandler(SessionContext sessionContext) => _sessionContext = sessionContext;

		public override Task<SetBreakpointsResponse> Handle(SetBreakpointsArguments request, CancellationToken cancellationToken)
		{
			List<OmniSharp.Extensions.DebugAdapter.Protocol.Models.Breakpoint> resultBps = new List<OmniSharp.Extensions.DebugAdapter.Protocol.Models.Breakpoint>();

			if (!request.Source.SourceReference.HasValue || request.Source.SourceReference.Value == 0)
			{
				string? sourceFilePath = request.Source.Path;
				if (sourceFilePath != null && request.Breakpoints is Container<SourceBreakpoint> bpsSource)
				{
					BreakpointStore bps = _sessionContext.DebuggerSession.Breakpoints;
					foreach (SourceBreakpoint bpSource in bpsSource)
					{
						int line = bpSource.Line;
						int column = bpSource.Column ?? 1;
						Mono.Debugging.Client.Breakpoint? bp = bps.GetBreakpointsAtFileLine(sourceFilePath, line).FirstOrDefault(x => x.Column == column);
						if (bp != null)
							bps.Remove(bp);
						else
						{
							bps.Add(sourceFilePath, line, column);
							resultBps.Add(new OmniSharp.Extensions.DebugAdapter.Protocol.Models.Breakpoint { Source = request.Source, Line = line, Column = column });
						}
					}
				}
			}

			return Task.FromResult(new SetBreakpointsResponse { Breakpoints = Container.From(resultBps) });
		}
	}

	class DisconnectHandler : DisconnectHandlerBase
	{
		private readonly SessionContext _sessionContext;
		public DisconnectHandler(SessionContext sessionContext) => _sessionContext = sessionContext;

		public override Task<DisconnectResponse> Handle(DisconnectArguments request, CancellationToken cancellationToken)
		{
			//_server.SendTerminated(new TerminatedEvent());
			_sessionContext.DebuggerSession.Exit();
			Program.ExitSignal.TrySetResult(null);
			return Task.FromResult(new DisconnectResponse());
		}
	}

	class StackTraceHandler : StackTraceHandlerBase
	{
		private readonly SessionContext _sessionContext;
		public StackTraceHandler(SessionContext sessionContext) => _sessionContext = sessionContext;

		public override Task<StackTraceResponse> Handle(StackTraceArguments request, CancellationToken cancellationToken)
		{
			static (long ProcessId, ThreadInfo? ThreadInfo) FindThread(ProcessInfo[] processes, long threadId)
			{
				foreach (ProcessInfo process in processes)
					foreach (ThreadInfo thread in process.GetThreads())
						if (thread.Id == threadId)
							return (process.Id, thread);
				return default;
			}
			(long processId, ThreadInfo? threadInfo) = FindThread(_sessionContext.DebuggerSession.GetProcesses(), request.ThreadId);
			if (threadInfo == default)
				return Task.FromResult(new StackTraceResponse { TotalFrames = 0 });

			Backtrace bt = threadInfo.Backtrace;
			int frameCount = bt.FrameCount;
			OmniSharp.Extensions.DebugAdapter.Protocol.Models.StackFrame[] resultFrames = new OmniSharp.Extensions.DebugAdapter.Protocol.Models.StackFrame[frameCount];
			for (int a = 0; a < frameCount; a++)
			{
				Mono.Debugging.Client.StackFrame sfSource = bt.GetFrame(a);
				SourceLocation loc = sfSource.SourceLocation;
				resultFrames[a] = new OmniSharp.Extensions.DebugAdapter.Protocol.Models.StackFrame { Id = a, InstructionPointerReference = string.Concat(sfSource.FullStackframeText, ":0x", sfSource.Address.ToString("x")), Source = new Source { Path = loc.FileName, SourceReference = _sessionContext.FrameReferenceManager.Encode(processId, threadInfo.Id, a) }, Line = loc.Line, EndLine = loc.EndLine, Column = loc.Column, EndColumn = loc.EndColumn };
			}
			return Task.FromResult(new StackTraceResponse { TotalFrames = frameCount, StackFrames = Container<OmniSharp.Extensions.DebugAdapter.Protocol.Models.StackFrame>.From(resultFrames) });
		}
	}

	class VariablesHandler : VariablesHandlerBase
	{
		private readonly SessionContext _sessionContext;
		public VariablesHandler(SessionContext sessionContext) => _sessionContext = sessionContext;

		public override Task<VariablesResponse> Handle(VariablesArguments request, CancellationToken cancellationToken)
		{
			static ObjectValue[]? GetObjectValues(long reference, SessionContext sessionContext)
			{
				if (reference == 0)
					return null;

				//VariablesReference might be either FrameReference...
				if (FrameReferenceManager.IsFrameReference(reference))
				{
					(long processId, long threadId, int frameId) = sessionContext.FrameReferenceManager.Decode(reference);
					if (!TryFindThread(sessionContext.DebuggerSession.GetProcesses(), processId, threadId, out ThreadInfo? threadInfo))
						return null;
					return threadInfo.Backtrace.GetFrame(frameId).GetAllLocals();
				}
				else
				//... or VariablesReference
				{
					if (!sessionContext.VariableReferenceManager.TryGet(reference, out ObjectValue? objectValue))
						return null;
					return objectValue.GetAllChildren();
				}
			}

			ObjectValue[]? objectValues = GetObjectValues(request.VariablesReference, _sessionContext);
			return Task.FromResult(objectValues == null
				? new VariablesResponse()
				: new VariablesResponse
					{
						Variables = Container<Variable>.From(
							Array.ConvertAll(objectValues,
								sourceLocal => new Variable { Name = sourceLocal.Name, Value = sourceLocal.Value, Type = sourceLocal.TypeName, VariablesReference = sourceLocal.HasChildren ? _sessionContext.VariableReferenceManager.Add(sourceLocal) : 0 }
							))
					});
		}

		internal static bool TryFindThread(ProcessInfo[] processes, long processId, long threadId, [NotNullWhen(true)] out ThreadInfo? threadInfo)
		{
			foreach (ProcessInfo process in processes)
				if (process.Id == processId)
				{
					foreach (ThreadInfo thread in process.GetThreads())
						if (thread.Id == threadId)
						{
							threadInfo = thread;
							return true;
						}
					break;
				}
			threadInfo = default;
			return false;
		}
	}

	class EvaluateHandler : EvaluateHandlerBase
	{
		private readonly SessionContext _sessionContext;
		public EvaluateHandler(SessionContext sessionContext) => _sessionContext = sessionContext;

		public override Task<EvaluateResponse> Handle(EvaluateArguments request, CancellationToken cancellationToken)
		{
			(long processId, long threadId, int frameId) = _sessionContext.FrameReferenceManager.Decode(request.FrameId ?? 0);
			if (!VariablesHandler.TryFindThread(_sessionContext.DebuggerSession.GetProcesses(), processId, threadId, out ThreadInfo? threadInfo))
				return Task.FromResult(new EvaluateResponse());
			ObjectValue val = threadInfo.Backtrace.GetFrame(frameId).GetExpressionValue(request.Expression, true);
			return Task.FromResult(new EvaluateResponse { Result = val.Value, Type = val.TypeName, VariablesReference = val.HasChildren ? _sessionContext.VariableReferenceManager.Add(val) : 0 });
		}
	}

	class TerminateHandler : TerminateHandlerBase
	{
		private readonly SessionContext _sessionContext;
		public TerminateHandler(SessionContext sessionContext) => _sessionContext = sessionContext;

		public override Task<TerminateResponse> Handle(TerminateArguments request, CancellationToken cancellationToken)
		{
			_sessionContext.DebuggerSession.Exit();
			return Task.FromResult(new TerminateResponse());
		}
	}

	class ContinueHandler : ContinueHandlerBase
	{
		private readonly SessionContext _sessionContext;
		public ContinueHandler(SessionContext sessionContext) => _sessionContext = sessionContext;

		public override Task<ContinueResponse> Handle(ContinueArguments request, CancellationToken cancellationToken)
		{
			_sessionContext.DebuggerSession.Continue();
			_sessionContext.ClearReferences();
			return Task.FromResult(new ContinueResponse());
		}
	}

	class StepInHandler : StepInHandlerBase
	{
		private readonly SessionContext _sessionContext;
		public StepInHandler(SessionContext sessionContext) => _sessionContext = sessionContext;

		public override Task<StepInResponse> Handle(StepInArguments request, CancellationToken cancellationToken)
		{
			_sessionContext.DebuggerSession.StepLine();
			_sessionContext.ClearReferences();
			return Task.FromResult(new StepInResponse());
		}
	}

	class StepOutHandler : StepOutHandlerBase
	{
		private readonly SessionContext _sessionContext;
		public StepOutHandler(SessionContext sessionContext) => _sessionContext = sessionContext;

		public override Task<StepOutResponse> Handle(StepOutArguments request, CancellationToken cancellationToken)
		{
			_sessionContext.DebuggerSession.Finish();
			_sessionContext.ClearReferences();
			return Task.FromResult(new StepOutResponse());
		}
	}

	class StepOverHandler : NextHandlerBase
	{
		private readonly SessionContext _sessionContext;
		public StepOverHandler(SessionContext sessionContext) => _sessionContext = sessionContext;

		public override Task<NextResponse> Handle(NextArguments request, CancellationToken cancellationToken)
		{
			_sessionContext.DebuggerSession.NextLine();
			_sessionContext.ClearReferences();
			return Task.FromResult(new NextResponse());
		}
	}

	enum InterfaceType
	{
		StdIo,
		Tcp,
		NamedPipe,
	}
}


using System.Diagnostics;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.DebugAdapter.Client;
using OmniSharp.Extensions.DebugAdapter.Protocol.Events;
using OmniSharp.Extensions.DebugAdapter.Protocol.Models;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace ATuimStudio.Debugger.Dap.Client
{
	sealed class SessionContext
	{
		public DebugAdapterClient Client { get; set; } = default!;
		public DapClient DapClient { get; set; } = default!;
	}

	public record struct TargetEventArgs(Backtrace? Backtrace);
	public class Backtrace(StackTraceResponse stackTrace, Func<OmniSharp.Extensions.DebugAdapter.Protocol.Models.StackFrame, CancellationToken, Task<VariablesResponse>> variablesProvider, Func<long, CancellationToken, Task<VariablesResponse>> childrenVariablesProvider, Func<OmniSharp.Extensions.DebugAdapter.Protocol.Models.StackFrame, string, CancellationToken, Task<EvaluateResponse>> expressionVariableProvider)
	{
		public int FrameCount => (int)(stackTrace.TotalFrames ?? 0);

		public OmniSharp.Extensions.DebugAdapter.Protocol.Models.StackFrame GetFrame(int frameIndex)
			=> stackTrace.StackFrames!.ElementAt(frameIndex);

		public Func<OmniSharp.Extensions.DebugAdapter.Protocol.Models.StackFrame, CancellationToken, Task<VariablesResponse>> VariablesProvider => variablesProvider;
		public Func<long, CancellationToken, Task<VariablesResponse>> ChildrenVariablesProvider => childrenVariablesProvider;
		public Func<OmniSharp.Extensions.DebugAdapter.Protocol.Models.StackFrame, string, CancellationToken, Task<EvaluateResponse>> ExpressionVariableProvider => expressionVariableProvider;
	};

	public sealed class DapClient : IDisposable
	{
		#region static creators
		public static DapClient FromStdIo(string adapterPath, string? otherParms)
			=> new DapClient(CreateStdIo(adapterPath, otherParms));

		public static async Task<DapClient> FromTcpAsync(string adapterPath, string? otherParms, IPAddress ipAddress, int port, CancellationToken cancellationToken)
			=> new DapClient(await CreateTcpAsync(adapterPath, otherParms, port, tcpClient => tcpClient.ConnectAsync(ipAddress, port, cancellationToken)));

		public static async Task<DapClient> FromTcpAsync(string adapterPath, string? otherParms, string host, int port, CancellationToken cancellationToken)
			=> new DapClient(await CreateTcpAsync(adapterPath, otherParms, port, tcpClient => tcpClient.ConnectAsync(host, port, cancellationToken)));

		public static async Task<DapClient> FromNamedPipeAsync(string adapterPath, string? otherParms, string pipeName, CancellationToken cancellationToken)
			=> new DapClient(await CreateNamedPipeAsync(adapterPath, otherParms, pipeName, cancellationToken));

		static (Action<DebugAdapterClientOptions> DebugAdapterClientInitializer, Action Finalizer) CreateStdIo(string adapterPath, string? otherParms)
		{
			Process adapter = StartAdapter(adapterPath, "--InterfaceType=StdIo", otherParms, static parms => { parms.RedirectStandardInput = true; parms.RedirectStandardOutput = true; });

			return (
				options => options
					.WithInput(adapter.StandardOutput.BaseStream)
					.WithOutput(adapter.StandardInput.BaseStream),
				() =>
				{
					adapter.Dispose();
					if (!adapter.HasExited)
						adapter.Kill(entireProcessTree: true);
				}
			);
		}

		static async Task<(Action<DebugAdapterClientOptions> DebugAdapterClientInitializer, Action Finalizer)> CreateTcpAsync(string adapterPath, string? otherParms, int port, Func<TcpClient, ValueTask> connectAction)
		{
			Process adapter = StartAdapter(adapterPath, "--InterfaceType=Tcp --Port=" + port, otherParms);

			TcpClient client = new TcpClient();
			await connectAction(client);
			NetworkStream stream = client.GetStream();

			return (
						options => options
							.WithInput(stream)
							.WithOutput(stream),
						() =>
						{
							adapter.Dispose();
							stream.Dispose();
							client.Dispose();
						}
			);
		}

		static async Task<(Action<DebugAdapterClientOptions> DebugAdapterClientInitializer, Action Finalizer)> CreateNamedPipeAsync(string adapterPath, string? otherParms, string pipeName, CancellationToken cancellationToken)
		{
			Process adapter = StartAdapter(adapterPath, "--InterfaceType=NamedPipe --NamedPipeName=" + pipeName, otherParms);

			NamedPipeClientStream pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
			await pipe.ConnectAsync(cancellationToken);
			return (
						options => options
							.WithInput(pipe)
							.WithOutput(pipe),
						pipe.Dispose
					);
		}

		static Process StartAdapter(string adapterPath, string arguments, string? otherParms)
			=> StartAdapter(adapterPath, arguments, otherParms, null);
		static Process StartAdapter(string adapterPath, string arguments, string? otherParms, Action<ProcessStartInfo>? parmsInitializer)
		{
			if (!string.IsNullOrEmpty(otherParms))
				arguments = string.Concat(arguments, " ", otherParms);

			ProcessStartInfo parms = new ProcessStartInfo(adapterPath)
			{
				Arguments = arguments,
				UseShellExecute = false,
				//CreateNoWindow = true
			};
			parmsInitializer?.Invoke(parms);
			return Process.Start(parms) ?? throw new InvalidOperationException("Debug adapter have not started.");
		}
		#endregion static creators

		readonly Action<DebugAdapterClientOptions> _debugAdapterClientInitializer;
		readonly Action _finalizer;
		DebugAdapterClient? _client;
		private DapClient((Action<DebugAdapterClientOptions> DebugAdapterClientInitializer, Action Finalizer) parms)
		{
			_debugAdapterClientInitializer = parms.DebugAdapterClientInitializer;
			_finalizer = parms.Finalizer;
		}

		public async Task StartAsync(string path, string? arguments, IEnumerable<KeyValuePair<string, string>>? environmentVariables, CancellationToken cancellationToken)
		{
			DebugAdapterClient client = DebugAdapterClient.Create(options =>
			{
				options
					.WithServices(services => services.AddSingleton(this).AddSingleton<SessionContext>())
					//.WithHandler<OutputHandler>()
					.WithHandler<TerminatedHandler>()
					.WithHandler<StoppedNotificationHandler>()
					;
				_debugAdapterClientInitializer(options);
			});
			_client = client;

			SessionContext sessionContext = client.GetRequiredService<SessionContext>();
			sessionContext.Client = client;
			sessionContext.DapClient = this;

			await client.Initialize(cancellationToken);
			await client.RequestConfigurationDone(new ConfigurationDoneArguments(), cancellationToken);

			InvokeInitialized();

			string cwd = Path.GetDirectoryName(path)!;
			Dictionary<string, object> startArgs = new Dictionary<string, object>
			{
				["program"] = path,
				["cwd"] = cwd,
				//["stopOnEntry"] = true
			};
			if (arguments != null)
				startArgs["args"] = new JArray(arguments.Split(" "));
			//if (environmentVariables != null)
			//	startArgs["env"] = new JArray(environmentVariables);
			LaunchRequestArguments launchArguments = new LaunchRequestArguments
			{
				NoDebug = false,
				ExtensionData = startArgs
			};
			await client.Launch(launchArguments, CancellationToken.None);
		}

		DebugAdapterClient Client => _client ?? throw new InvalidOperationException("Client has not started.");

		public event EventHandler? Initialized;
		internal void InvokeInitialized()
			=> Initialized?.Invoke(this, EventArgs.Empty);
		public event EventHandler<TargetEventArgs>? TargetHitBreakpoint;
		internal void InvokeTargetHitBreakpoint(TargetEventArgs args)
			=> TargetHitBreakpoint?.Invoke(this, args);
		public event EventHandler<TargetEventArgs>? TargetStopped;
		internal void InvokeTargetStopped(TargetEventArgs args)
			=> TargetStopped?.Invoke(this, args);
		public event EventHandler<TargetEventArgs>? TargetExited;
		internal void InvokeTargetExited(TargetEventArgs args)
			=> TargetExited?.Invoke(this, args);

		public void Dispose()
		{
			_finalizer();
		}

		public Task Continue(CancellationToken cancellationToken)
			=> Client.RequestContinue(new ContinueArguments(), cancellationToken);

		public Task StepIn(CancellationToken cancellationToken)
			=> Client.RequestStepIn(new StepInArguments(), cancellationToken);

		public Task StepOut(CancellationToken cancellationToken)
			=> Client.RequestStepOut(new StepOutArguments(), cancellationToken);

		public Task StepOver(CancellationToken cancellationToken)
			=> Client.RequestNext(new NextArguments(), cancellationToken);

		public Task Terminate(CancellationToken cancellationToken)
			=> Client.RequestTerminate(new TerminateArguments { Restart = false }, cancellationToken);

		public Task SetBreakpoints(string path, IEnumerable<(int Line, int Column)> positions, CancellationToken cancellationToken)
			=> Client.SetBreakpoints(new SetBreakpointsArguments
			{
				Source = new Source
				{
					SourceReference = 0,
					Path = path
				},
				Breakpoints = Container.From(positions.Select(static x => new SourceBreakpoint { Line = x.Line, Column = x.Column }))
			}, cancellationToken);
	}

	class TerminatedHandler : TerminatedHandlerBase
	{
		readonly SessionContext _sessionContext;
		public TerminatedHandler(SessionContext sessionContext) => _sessionContext = sessionContext;

		public override Task<Unit> Handle(TerminatedEvent request, CancellationToken cancellationToken)
		{
			_sessionContext.DapClient.InvokeTargetExited(new TargetEventArgs());
			return Unit.Task;
		}
	}

	class StoppedNotificationHandler : StoppedHandlerBase
	{
		readonly SessionContext _sessionContext;
		public StoppedNotificationHandler(SessionContext sessionContext) => _sessionContext = sessionContext;
	
		public override async Task<Unit> Handle(StoppedEvent request, CancellationToken cancellationToken)
		{
			Backtrace? backtrace = null;
			if (request.ThreadId.HasValue)
			{
				StackTraceResponse stackTrace = await _sessionContext.Client.RequestStackTrace(new StackTraceArguments { ThreadId = request.ThreadId.Value }, cancellationToken: cancellationToken);
				if (stackTrace.StackFrames is not null)
					backtrace = new Backtrace(stackTrace,
						(stackFrame, cancellationToken) => _sessionContext.Client.RequestVariables(new VariablesArguments { VariablesReference = stackFrame.Source!.SourceReference!.Value }, cancellationToken),
						(variablesReference, cancellationToken) => _sessionContext.Client.RequestVariables(new VariablesArguments { VariablesReference = variablesReference }, cancellationToken),
						(stackFrame, expression, cancellationToken) => _sessionContext.Client.RequestEvaluate(new EvaluateArguments { FrameId = stackFrame.Source!.SourceReference!.Value, Expression = expression } ,cancellationToken)
						);
			}
			_sessionContext.DapClient.InvokeTargetStopped(new TargetEventArgs(backtrace));
			return Unit.Value;
		}
	}
}

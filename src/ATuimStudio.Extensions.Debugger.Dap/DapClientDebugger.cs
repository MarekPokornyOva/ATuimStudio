using ATuimStudio.Debugger.Dap.Client;
using ATuimStudio.Extensions.Debug;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;
using System.Collections;

namespace ATuimStudio.Extensions.Debugger.Dap
{
	sealed class DapClientDebugger : IDebugger, IDisposable
	{
		BreakpointEventArgs? _currentBreakpoint;
		DapClient _dapClient = default!;
		readonly Func<CancellationToken, Task<DapClient>> _dapClientFactory;
		readonly string _path;
		readonly string? _arguments;
		readonly IEnumerable<KeyValuePair<string, string>>? _environmentVariables;

		public DapClientDebugger(Func<CancellationToken, Task<DapClient>> dapClientFactory, string path, string? arguments, IEnumerable<KeyValuePair<string, string>>? environmentVariables)
		{
			_dapClientFactory = dapClientFactory;
			_path = path;
			_arguments = arguments;
			_environmentVariables = environmentVariables;
		}

		void Session_TargetStopped(object? sender, TargetEventArgs e)
		{
			if (e.Backtrace != null)
				OnBreakpoint?.Invoke(this, _currentBreakpoint = CreateBreakpointArgs(e.Backtrace));
		}
		void Session_TargetExited(object? sender, TargetEventArgs e)
		{
			OnTerminated?.Invoke(this, TerminatedEventArgs.Instance);
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			_dapClient = await _dapClientFactory(cancellationToken);

			_dapClient.Initialized += Session_Initialized;
			_dapClient.TargetHitBreakpoint += Session_TargetStopped;
			_dapClient.TargetStopped += Session_TargetStopped;
			_dapClient.TargetExited += Session_TargetExited;

			await _dapClient.StartAsync(_path, _arguments, _environmentVariables, cancellationToken);
		}

		static BreakpointEventArgs CreateBreakpointArgs(Backtrace backtrace)
			=> new BreakpointEventArgs(new Stack(backtrace));

		void Session_Initialized(object? sender, EventArgs e)
			=> OnStarted?.Invoke(this, _currentBreakpoint = new BreakpointEventArgs([]));

		public event EventHandler<BreakpointEventArgs>? OnStarted;
		public event EventHandler<BreakpointEventArgs>? OnBreakpoint;
		public event EventHandler<BreakpointEventArgs>? OnContinued;
		public event EventHandler<TerminatedEventArgs>? OnTerminated;
#pragma warning disable CS0067
		public event EventHandler<string>? OnStandardOut;
		public event EventHandler<string>? OnStandardError;
#pragma warning restore CS0067
		//public event EventHandler<string>? OnStandardOut
		//{
		//	add => _debugger.OnStandardOut += value;
		//	remove => _debugger.OnStandardOut -= value;
		//}
		//public event EventHandler<string>? OnStandardError
		//{
		//	add => _debugger.OnStandardError += value;
		//	remove => _debugger.OnStandardError -= value;
		//}
		//public StreamWriter StandardInput => _debugger.StandardInput;
		public StreamWriter StandardInput { get; } = new StreamWriter(new MemoryStream());

		public void Dispose()
		{
			DapClient session = _dapClient;
			session.Initialized -= Session_Initialized;
			session.TargetHitBreakpoint -= Session_TargetStopped;
			session.TargetStopped -= Session_TargetStopped;
			session.TargetExited -= Session_TargetExited;
			session.Dispose();
		}

		public Task ContinueAsync(CancellationToken cancellationToken)
		{
			OnContinued?.Invoke(this, _currentBreakpoint!);
			return _dapClient.Continue(cancellationToken);
		}

		//public void Pause()
		//{
		//	_session.Stop();
		//}

		public Task StepInAsync(CancellationToken cancellationToken)
		{
			OnContinued?.Invoke(this, _currentBreakpoint!);
			return _dapClient.StepIn(cancellationToken);
		}

		public Task StepOutAsync(CancellationToken cancellationToken)
		{
			OnContinued?.Invoke(this, _currentBreakpoint!);
			return _dapClient.StepOut(cancellationToken);
		}

		public Task StepOverAsync(CancellationToken cancellationToken)
		{
			OnContinued?.Invoke(this, _currentBreakpoint!);
			return _dapClient.StepOver(cancellationToken);
		}

		public Task TerminateAsync(CancellationToken cancellationToken)
		{
			OnContinued?.Invoke(this, _currentBreakpoint!);
			return _dapClient.Terminate(cancellationToken);
		}

		public async Task<bool> ToggleBreakpointAsync(string sourceFilePath, SourcePosition position, CancellationToken cancellationToken)
		{
			await _dapClient.SetBreakpoints(sourceFilePath, [(position.Line, position.Column)], cancellationToken).ConfigureAwait(false);
			return true;
		}

		sealed class Stack : IReadOnlyList<IStackFrame>, IEnumerator<IStackFrame>
		{
			readonly Backtrace _backtrace;
			readonly int _count;
			public Stack(Backtrace backtrace)
			{
				_backtrace = backtrace;
				_count = _backtrace.FrameCount;
			}

			#region IReadOnlyList
			public int Count => _count;

			public IEnumerator<IStackFrame> GetEnumerator()
			{
				Reset();
				return this;
			}

			IEnumerator IEnumerable.GetEnumerator()
				=> GetEnumerator();

			public IStackFrame this[int index] => Map(index);
			#endregion IReadOnlyList

			#region IEnumerator
			int _index = 0;
			public IStackFrame Current { get; private set; } = default!;

			object IEnumerator.Current => Current;

			public bool MoveNext()
			{
				if (_index == _count)
					return false;

				Current = Map(_index++);
				return true;
			}

			public void Reset()
			{
				_index = 0;
				Current = default!;
			}

			public void Dispose()
			{
			}
			#endregion IEnumerator

			StackFrame Map(int index)
				=> new StackFrame(_backtrace.GetFrame(index), _backtrace.VariablesProvider, _backtrace.ChildrenVariablesProvider, _backtrace.ExpressionVariableProvider);
		}

		class StackFrame : IStackFrame
		{
			readonly OmniSharp.Extensions.DebugAdapter.Protocol.Models.StackFrame _inner;
			readonly Func<OmniSharp.Extensions.DebugAdapter.Protocol.Models.StackFrame, CancellationToken, Task<VariablesResponse>> _variablesProvider;
			readonly Func<long, CancellationToken, Task<VariablesResponse>> _childrenVariablesProvider;
			readonly Func<OmniSharp.Extensions.DebugAdapter.Protocol.Models.StackFrame, string, CancellationToken, Task<EvaluateResponse>> _expressionVariableProvider;
			internal StackFrame(
				OmniSharp.Extensions.DebugAdapter.Protocol.Models.StackFrame inner,
				Func<OmniSharp.Extensions.DebugAdapter.Protocol.Models.StackFrame, CancellationToken, Task<VariablesResponse>> variablesProvider,
				Func<long, CancellationToken, Task<VariablesResponse>> childrenVariablesProvider,
				Func<OmniSharp.Extensions.DebugAdapter.Protocol.Models.StackFrame, string, CancellationToken, Task<EvaluateResponse>> expressionVariableProvider
				)
			{
				_inner = inner;
				_variablesProvider = variablesProvider;
				_childrenVariablesProvider = childrenVariablesProvider;
				_expressionVariableProvider = expressionVariableProvider;
			}

			public string SourceFilePath => _inner.Source!.Path!;

			public SourceRange? Range { get => new SourceRange(_inner.Line, _inner.Column, _inner.EndLine ?? _inner.Line, _inner.EndColumn ?? _inner.Column); }

			static readonly Task<IReadOnlyCollection<IDebugItem>> _emptyArguments = Task.FromResult<IReadOnlyCollection<IDebugItem>>([]);
			public Task<IReadOnlyCollection<IDebugItem>> GetArgumentsAsync(CancellationToken cancellationToken)
				=> _emptyArguments;

			public async Task<IReadOnlyCollection<IDebugItem>> GetLocalsAsync(CancellationToken cancellationToken)
				=> MapVariables(await _variablesProvider(_inner, cancellationToken).ConfigureAwait(false), _childrenVariablesProvider);

			internal static IReadOnlyCollection<IDebugItem> MapVariables(VariablesResponse variablesResponse, Func<long, CancellationToken, Task<VariablesResponse>> childrenVariablesProvider)
			{
				OmniSharp.Extensions.DebugAdapter.Protocol.Models.Container<OmniSharp.Extensions.DebugAdapter.Protocol.Models.Variable>? vars = variablesResponse.Variables;
				return vars is null
					? []
					: [.. vars.Select(x => new ReadOnlyDebugItem(x, childrenVariablesProvider))];
			}

			public async Task<IDebugItem> EvaluateAsync(string expression, CancellationToken cancellationToken)
				=> new ExpressionDebugItem(expression, await _expressionVariableProvider(_inner, expression, cancellationToken), _childrenVariablesProvider);

			public string FullStackFrameText
				=> _inner.InstructionPointerReference ?? "";
		}

		readonly struct ReadOnlyDebugItem : IDebugItem
		{
			readonly OmniSharp.Extensions.DebugAdapter.Protocol.Models.Variable _inner;
			readonly Func<long, CancellationToken, Task<VariablesResponse>> _childrenProvider;

			public ReadOnlyDebugItem(OmniSharp.Extensions.DebugAdapter.Protocol.Models.Variable inner, Func<long, CancellationToken, Task<VariablesResponse>> childrenProvider)
			{
				_inner = inner;
				_childrenProvider = childrenProvider;
			}

			public readonly string Name => _inner.Name;
			public readonly string Value => _inner.Value;
			public readonly string TypeName => _inner.Type ?? "";
			public event EventHandler ValueChanged { add { } remove { } }
			public readonly bool HasChildren => _inner.VariablesReference != 0;
			public IReadOnlyCollection<IDebugItem> GetAllChildren()
				=> StackFrame.MapVariables(_childrenProvider(_inner.VariablesReference, CancellationToken.None).AwaitSync(false), _childrenProvider);
		}

		readonly struct ExpressionDebugItem : IDebugItem
		{
			readonly OmniSharp.Extensions.DebugAdapter.Protocol.Requests.EvaluateResponse _inner;
			readonly Func<long, CancellationToken, Task<VariablesResponse>> _childrenProvider;

			public ExpressionDebugItem(string expression, OmniSharp.Extensions.DebugAdapter.Protocol.Requests.EvaluateResponse inner, Func<long, CancellationToken, Task<VariablesResponse>> childrenProvider)
			{
				Expression = expression;
				_inner = inner;
				_childrenProvider = childrenProvider;
			}

			public readonly string Name => Expression;
			public readonly string Value => _inner.Result;
			public readonly string TypeName => _inner.Type ?? "";
			public event EventHandler ValueChanged { add { } remove { } }
			public readonly string Expression { get; }
			public readonly bool HasChildren => _inner.VariablesReference != 0;
			public IReadOnlyCollection<IDebugItem> GetAllChildren()
				=> StackFrame.MapVariables(_childrenProvider(_inner.VariablesReference, CancellationToken.None).AwaitSync(false), _childrenProvider);
		}
	}
}

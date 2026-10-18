using ATuimStudio.Extensions.Debug;
using AvalonStudio.Debugging.DotNetCore;
using Mono.Debugging.Client;
using System.Collections;
using BreakpointEventArgs = ATuimStudio.Extensions.Debug.BreakpointEventArgs;

namespace ATuimStudio.Extensions.Debugger.Mono
{
	sealed class MonoDebugger : IDebugger
	{
		readonly DebuggerSession _session;
		BreakpointEventArgs? _currentBreakpoint;
		readonly Action _start;

		public MonoDebugger(string path, string? arguments, IEnumerable<KeyValuePair<string, string>>? environmentVariables)
		{
			DebuggerSession session = DotNetCoreDebugger.CreateSession();
			_session = session;

			//session.TargetEvent += static (sender, e) => { };
			//session.TargetUnhandledException += static (sender, e) => { };
			session.TargetHitBreakpoint += Session_TargetHitBreakpoint;
			session.TargetStopped += Session_TargetStopped;
			session.TargetExited += Session_TargetExited;

			_start = () =>
			{
				OnStarted?.Invoke(this, _currentBreakpoint = new BreakpointEventArgs([]));
				DebuggerStartInfo startinfo = DotNetCoreDebugger.GetDebuggerStartInfo(path, arguments, environmentVariables);
				_session.Run(startinfo, DotNetCoreDebugger.GetDebuggerSessionOptions());
			};
		}

		void Session_TargetHitBreakpoint(object? sender, TargetEventArgs e)
			=> OnBreakpoint?.Invoke(this, _currentBreakpoint = CreateBreakpointArgs(e.Backtrace));
		void Session_TargetStopped(object? sender, TargetEventArgs e)
		{
			if (e.Backtrace != null)
				OnBreakpoint?.Invoke(this, _currentBreakpoint = CreateBreakpointArgs(e.Backtrace));
		}
		void Session_TargetExited(object? sender, TargetEventArgs e)
		{
			((DebuggerSession)sender!).Exit();
			OnTerminated?.Invoke(this, TerminatedEventArgs.Instance);
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_start();
			return Task.CompletedTask;
		}

		static BreakpointEventArgs CreateBreakpointArgs(Backtrace backtrace)
			=> new BreakpointEventArgs(new Stack(backtrace));

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
			DebuggerSession session = _session;
			session.TargetHitBreakpoint -= Session_TargetHitBreakpoint;
			session.TargetStopped -= Session_TargetStopped;
			session.TargetExited -= Session_TargetExited;
			session.Dispose();
		}

		public Task ContinueAsync(CancellationToken cancellationToken)
		{
			OnContinued?.Invoke(this, _currentBreakpoint!);
			_session.Continue();
			return Task.CompletedTask;
		}

		//public void Pause()
		//{
		//	_session.Stop();
		//}

		public Task StepInAsync(CancellationToken cancellationToken)
		{
			OnContinued?.Invoke(this, _currentBreakpoint!);
			_session.StepLine();
			return Task.CompletedTask;
		}

		public Task StepOutAsync(CancellationToken cancellationToken)
		{
			OnContinued?.Invoke(this, _currentBreakpoint!);
			_session.Finish();
			return Task.CompletedTask;
		}

		public Task StepOverAsync(CancellationToken cancellationToken)
		{
			OnContinued?.Invoke(this, _currentBreakpoint!);
			_session.NextLine();
			return Task.CompletedTask;
		}

		public Task TerminateAsync(CancellationToken cancellationToken)
		{
			OnContinued?.Invoke(this, _currentBreakpoint!);
			_session.Exit();
			return Task.CompletedTask;
		}

		public Task<bool> ToggleBreakpointAsync(string sourceFilePath, SourcePosition position, CancellationToken cancellationToken)
		{
			if (_session == null)
				return Task.FromResult(false);

			BreakpointStore bps = _session.Breakpoints;
			global::Mono.Debugging.Client.Breakpoint? bp = bps.GetBreakpointsAtFileLine(sourceFilePath, position.Line).FirstOrDefault(x => x.Column == position.Column);
			if (bp != null)
				return Task.FromResult(bps.Remove(bp));

			return Task.FromResult(bps.Add(sourceFilePath, position.Line, position.Column) != null);
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
				=> new StackFrame(_backtrace.GetFrame(index));
		}

		class StackFrame : IStackFrame
		{
			readonly global::Mono.Debugging.Client.StackFrame _inner;
			internal StackFrame(global::Mono.Debugging.Client.StackFrame inner)
				=> _inner = inner;

			public string SourceFilePath => _inner.SourceLocation.FileName;

			public SourceRange? Range { get { SourceLocation loc = _inner.SourceLocation; return new SourceRange(loc.Line, loc.Column, loc.EndLine, loc.EndColumn); } }

			public Task<IReadOnlyCollection<IDebugItem>> GetArgumentsAsync(CancellationToken cancellationToken)
				=> Task.FromResult<IReadOnlyCollection<IDebugItem>>([.. _inner.GetParameters().Select(static x => new ReadOnlyDebugItem(x))]);

			public Task<IReadOnlyCollection<IDebugItem>> GetLocalsAsync(CancellationToken cancellationToken)
				=> Task.FromResult<IReadOnlyCollection<IDebugItem>>([.. _inner.GetLocalVariables().Select(static x => new ReadOnlyDebugItem(x))]);

			public Task<IDebugItem> EvaluateAsync(string expression, CancellationToken cancellationToken)
				=> Task.FromResult<IDebugItem>(new ExpressionDebugItem(expression, _inner.GetExpressionValue(expression, true)));

#pragma warning disable CS0612
			public string FullStackFrameText => string.Concat(_inner.FullStackframeText, ":0x", _inner.Address.ToString("x"));
#pragma warning restore CS0612
		}

		readonly struct ReadOnlyDebugItem : IDebugItem
		{
			readonly ObjectValue _inner;

			public ReadOnlyDebugItem(ObjectValue inner)
			{
				_inner = inner;
			}

			public readonly string Name => _inner.Name;
			public readonly string Value => _inner.Value;
			public readonly string TypeName => _inner.TypeName;
			public event EventHandler ValueChanged { add { if (_inner.IsEvaluating) _inner.ValueChanged += value; } remove => _inner.ValueChanged -= value; }
			public readonly bool HasChildren => _inner.HasChildren;
			public IReadOnlyCollection<IDebugItem> GetAllChildren()
				=> new MappingReadOnlyCollection<ObjectValue, IDebugItem>(_inner.GetAllChildren(), x => new ReadOnlyDebugItem(x));
		}

		readonly struct ExpressionDebugItem : IDebugItem
		{
			readonly ObjectValue _inner;

			public ExpressionDebugItem(string expression, ObjectValue inner)
			{
				_inner = inner;
				Expression = expression;
			}

			public readonly string Name => _inner.Name;
			public readonly string Value => _inner.Value;
			public readonly string TypeName => _inner.TypeName;
			public event EventHandler ValueChanged { add { if (_inner.IsEvaluating) _inner.ValueChanged += value; } remove => _inner.ValueChanged -= value; }
			public readonly string Expression { get; }
			public readonly bool HasChildren => _inner.HasChildren;
			public IReadOnlyCollection<IDebugItem> GetAllChildren()
				=> new MappingReadOnlyCollection<ObjectValue, IDebugItem>(_inner.GetAllChildren(), x => new ReadOnlyDebugItem(x));
		}
	}
}

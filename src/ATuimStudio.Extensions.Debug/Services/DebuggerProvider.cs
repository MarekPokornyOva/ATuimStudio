using ATuimStudio.Extensions.Core;

namespace ATuimStudio.Extensions.Debug
{
	sealed class DebuggerProvider : IDebuggerProvider, IDebuggerFactory
	{
		readonly IServiceProvider _serviceProvider;
		readonly IUserOptions _userOptions;
		readonly IReadOnlyDictionary<string, Func<IServiceProvider, IDebuggerFactory>> _debuggerFactories;
		public DebuggerProvider(IServiceProvider serviceProvider, IUserOptions userOptions, IEnumerable<IDebuggerRegistrator> debuggerRegistrators)
		{
			_serviceProvider = serviceProvider;
			_userOptions = userOptions;
			Dictionary<string, Func<IServiceProvider, IDebuggerFactory>> debuggerFactories = new Dictionary<string, Func<IServiceProvider, IDebuggerFactory>>();
			foreach (IDebuggerRegistrator debuggerRegistrator in debuggerRegistrators)
			{
				(string key, Func<IServiceProvider, IDebuggerFactory> factory) = debuggerRegistrator.Register();
				debuggerFactories.Add(key, factory);
			}
			_debuggerFactories = debuggerFactories;
		}

		public IDebugger? Current
		{
			get;
			set
			{
				if (field != null && value != null)
					throw new InvalidOperationException("A debugger is already set.");
				field = value;
			}
		}

		public event EventHandler<EventArgs>? OnChanged;

		public async Task<IDebugger> CreateDebuggerAsync(string path, string? arguments, IEnumerable<KeyValuePair<string, string>>? environmentVariables, CancellationToken cancellationToken)
		{
			if (!_debuggerFactories.TryGetValue(_userOptions.GetString(UserOptionsCodes.Selected), out Func<IServiceProvider, IDebuggerFactory>? debuggerFactory))
				throw new InvalidOperationException("Selected debugger not registered.");

			IDebugger debugger;
			IDebuggerFactory dbgFact = debuggerFactory(_serviceProvider);
			try
			{
				debugger = await dbgFact.CreateDebuggerAsync(path, arguments, environmentVariables, cancellationToken);
			}
			finally
			{
				if (dbgFact is IDisposable disp)
					disp.Dispose();
			}

			Current = debugger;
			OnChanged?.Invoke(debugger, EventArgs.Empty);
			debugger.OnTerminated += DebuggerOnTerminated;
			return debugger;
		}

		void DebuggerOnTerminated(object? sender, TerminatedEventArgs e)
		{
			((IDebugger)sender!).OnTerminated -= DebuggerOnTerminated;
			Current = null;
			OnChanged?.Invoke(null!, EventArgs.Empty);
		}
	}
}

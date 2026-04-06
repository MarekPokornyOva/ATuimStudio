namespace ATuimStudio.Extensions.Core
{
	public sealed class DebouncingHandler : IDisposable
	{
		readonly Action<FileSystemEventArgs> _handler;
		readonly int _delayMs;
		FileSystemEventArgs? _lastArgs;
		readonly Timer _timer;
		public DebouncingHandler(Action<FileSystemEventArgs> handler, int delayMs)
		{
			_timer = new Timer(TimerHandler, this, Timeout.Infinite, Timeout.Infinite);

			_handler = handler;
			_delayMs = delayMs;
		}

		public void Handle(FileSystemEventArgs args)
		{
			//renamed or deleted file should be handled immediately
			if (args.ChangeType == WatcherChangeTypes.Renamed || args.ChangeType == WatcherChangeTypes.Deleted)
			{
				TimerStop(_timer);
				if (_lastArgs != null)
					_handler(_lastArgs);
				_handler(args);
				return;
			}

			//decide when to handle created or changed file
			if (_lastArgs == null)
				_lastArgs = args;
			else if (!FileSystemEventArgsCompatible(_lastArgs, args))
			{
				TimerStop(_timer);
				_handler(_lastArgs);
				_lastArgs = args;
			}
			_timer.Change(_delayMs, Timeout.Infinite);
		}

		static bool FileSystemEventArgsCompatible(FileSystemEventArgs x, FileSystemEventArgs y)
		{
			//if actual file is created, previous must be handled
			if (y.ChangeType == WatcherChangeTypes.Created)
				return false;

			//y.ChangeType is changed here
			//x.ChangeType is created or changed here
			return PathHelper.PathEqualityComparer.Equals(x.FullPath, y.FullPath);
		}

		static void TimerHandler(object state)
		{
			DebouncingHandler self = (DebouncingHandler)state;

			TimerStop(self._timer);
			if (self._lastArgs != null)
				self._handler(self._lastArgs);
			self._lastArgs = null;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		static void TimerStop(Timer timer)
			=> timer.Change(Timeout.Infinite, Timeout.Infinite);

		public void Dispose()
		{
			_timer.Dispose();
		}
	}
}

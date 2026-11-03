using ATuimStudio.Extensions.Core;
using System.Collections.Concurrent;

namespace ATuimStudio
{
	class ApplicationLifetimeService : IApplicationLifetimeService
	{
		readonly ConcurrentBag<WeakReference<IDisposable>> _disposables = new();
		readonly ConcurrentBag<WeakReference<IAsyncDisposable>> _asyncDisposables = new();

		public void RegisterDisposable(IDisposable disposable)
		{
			_disposables.Add(new WeakReference<IDisposable>(disposable));
		}

		public void RegisterDisposable(IAsyncDisposable disposable)
		{
			_asyncDisposables.Add(new WeakReference<IAsyncDisposable>(disposable));
		}

		internal void Dispose()
		{
			foreach (WeakReference<IDisposable> reference in _disposables)
				if (reference.TryGetTarget(out IDisposable? disposable))
					disposable.Dispose();

			foreach (WeakReference<IAsyncDisposable> reference in _asyncDisposables)
				if (reference.TryGetTarget(out IAsyncDisposable? disposable))
					disposable.DisposeAsync().AsTask().AwaitSync(false);

			_disposables.Clear();
			_asyncDisposables.Clear();
		}
	}
}

using System.Collections.Concurrent;

namespace ATuimStudio.Extensions.Core
{
	sealed class DefaultDiskWatchService : IDiskWatchService
	{
		readonly ConcurrentDictionary<string, (FileSystemWatcher Watcher, ConcurrentDictionary<Client, int> Clients)> _watchers = new ConcurrentDictionary<string, (FileSystemWatcher Watcher, ConcurrentDictionary<Client, int> Clients)>(PathHelper.PathEqualityComparer);

		public IWatchClient Watch(string path, Action<FileSystemEventArgs> handler, DiskWatchNotifyOptions? options)
		{
			(FileSystemWatcher watcher, ConcurrentDictionary<Client, int> clients) = _watchers.GetOrAdd(path, p =>
			{
				FileSystemWatcher watcher = new FileSystemWatcher
				{
					Filter = "*.*",
					Path = p,
					IncludeSubdirectories = true,
					EnableRaisingEvents = true,
				};
				watcher.Created += OnFileEvent;
				watcher.Changed += OnFileEvent;
				watcher.Deleted += OnFileEvent;
				watcher.Renamed += OnFileRenamedEvent;
				return (watcher, new ConcurrentDictionary<Client, int>());
			});

			Client client = new Client(options, handler, clientToRemove => clients.Remove(clientToRemove, out _));
			clients.AddOrUpdate(client, default(int), static (_, x) => x);

			return client;
		}

		void OnFileEvent(object sender, FileSystemEventArgs e)
			=> Process(e, e.FullPath);

		void OnFileRenamedEvent(object sender, RenamedEventArgs e)
			=> Process(e, e.OldFullPath);

		void Process(FileSystemEventArgs e, string path)
		{
			foreach (KeyValuePair<string, (FileSystemWatcher Watcher, ConcurrentDictionary<Client, int> Clients)> item in _watchers)
				if (PathHelper.IsRootPathOf(item.Key, path))
					foreach ((Client client, _) in item.Value.Clients)
						client.Notify(e);
		}

		sealed class Client : IWatchClient
		{
			readonly DiskWatchNotifyOptions? _options;
			readonly Action<FileSystemEventArgs> _handler;
			readonly Action<Client> _dispose;
			internal Client(DiskWatchNotifyOptions? options, Action<FileSystemEventArgs> handler, Action<Client> dispose)
			{
				_options = options;
				_handler = handler;
				_dispose = dispose;
			}

			public void Dispose()
				=> _dispose(this);

			internal void Notify(FileSystemEventArgs e)
			{
				if (_options?.Filter != null && _options.Filter(e))
					return;
				_handler(e);
			}
		}
	}
}

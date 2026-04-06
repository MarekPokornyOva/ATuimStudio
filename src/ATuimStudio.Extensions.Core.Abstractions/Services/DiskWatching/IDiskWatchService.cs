namespace ATuimStudio.Extensions.Core
{
	public interface IDiskWatchService
	{
		IWatchClient Watch(string path, Action<FileSystemEventArgs> handler, DiskWatchNotifyOptions? options);
	}

	public sealed class DiskWatchNotifyOptions
	{
		public Action<FileSystemEventArgs>? Debouncing { get; set; }
		public Func<FileSystemEventArgs, bool>? Filter { get; set; }
	}
}

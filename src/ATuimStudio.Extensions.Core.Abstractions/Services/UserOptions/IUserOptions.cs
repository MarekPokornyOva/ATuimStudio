namespace ATuimStudio.Extensions.Core
{
	public interface IUserOptionsImmutable
	{
		bool TryGetString(string key, out string? value);
		bool TryGetInt(string key, out int value);
		bool TryGetLong(string key, out long value);
		bool TryGetDouble(string key, out double value);
		bool TryGetBool(string key, out bool value);
		bool TryGetBytes(string key, out byte[]? value);
	}

	public interface IUserOptions : IUserOptionsImmutable
	{
		IUserOptionsEdit GetEdit();
		event EventHandler<IReadOnlyDictionary<string, object?>>? Changed;
	}

	public interface IUserOptionsEdit : IUserOptionsImmutable
	{
		void SetValue(string key, string value);
		void SetValue(string key, int value);
		void SetValue(string key, long value);
		void SetValue(string key, double value);
		void SetValue(string key, bool value);
		void SetValue(string key, byte[] value);

		void ResetToDefault(string key);
		void Apply();
	}

	public interface IUserOptionsRegistrator
	{
		void RegisterDefault(string key, string value);
		void RegisterDefault(string key, int value);
		void RegisterDefault(string key, long value);
		void RegisterDefault(string key, double value);
		void RegisterDefault(string key, bool value);
		void RegisterDefault(string key, byte[] value);
	}

	public interface IUserOptionsProvider
	{
		IUserOptions GetUserOptions();
	}

	public interface IUserOptionsManager : IUserOptions, IUserOptionsRegistrator, IUserOptionsProvider
	{
		Task LoadAsync(IUserOptionsRepository repository, CancellationToken cancellationToken);
	}

	public interface IUserOptionsRepository
	{
		IAsyncEnumerable<KeyValuePair<string, object>> LoadAsync(CancellationToken cancellationToken);
		Task SaveAsync(IEnumerable<KeyValuePair<string, object?>> values, CancellationToken cancellationToken);
		Task ClearAllAsync(CancellationToken cancellationToken);
	}

	public interface IUserSolutionOptionsRepository
	{
		IAsyncEnumerable<KeyValuePair<string, object>> LoadAsync(CancellationToken cancellationToken);
		Task SaveAsync(IEnumerable<KeyValuePair<string, object?>> values, CancellationToken cancellationToken);
		Task ClearAllAsync(CancellationToken cancellationToken);
	}	

	public static class UserOptionsImmutableGetExtensions
	{
		public static string GetString(this IUserOptionsImmutable options, string key)
			=> Get<string>(options.TryGetString, key);
		public static int GetInt(this IUserOptionsImmutable options, string key)
			=> Get<int>(options.TryGetInt, key);
		public static long GetLong(this IUserOptionsImmutable options, string key)
			=> Get<long>(options.TryGetLong, key);
		public static double GetDouble(this IUserOptionsImmutable options, string key)
			=> Get<double>(options.TryGetDouble, key);
		public static bool GetBool(this IUserOptionsImmutable options, string key)
			=> Get<bool>(options.TryGetBool, key);
		public static byte[] GetBytes(this IUserOptionsImmutable options, string key)
			=> Get<byte[]>(options.TryGetBytes, key);

		delegate bool TryGetDel<TResult>(string key, out TResult? arg);
		static T Get<T>(TryGetDel<T> tryGet, string key)
		{
			if (tryGet(key, out T? result))
				return result!;
			throw new KeyNotFoundException();
		}
	}
}

using ATuimStudio.Extensions.Core;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace ATuimStudio.Extensions.Core
{
	sealed class DefaultUserOptionsManager : IUserOptionsManager, IUserOptions
	{
		#region IUserOptionsManager
		readonly ConcurrentDictionary<string, object> _defaults = [];
		readonly ConcurrentDictionary<string, object> _values = [];

		public IUserOptions GetUserOptions()
			=> this;

		public event EventHandler<IReadOnlyDictionary<string, object?>>? Changed;

		public void RegisterDefault(string key, string value)
			=> _defaults[key] = value;

		public void RegisterDefault(string key, int value)
			=> _defaults[key] = value;

		public void RegisterDefault(string key, long value)
			=> _defaults[key] = value;

		public void RegisterDefault(string key, double value)
			=> _defaults[key] = value;

		public void RegisterDefault(string key, bool value)
			=> _defaults[key] = value;

		public void RegisterDefault(string key, byte[] value)
			=> _defaults[key] = value;

		public bool TryGetDouble(string key, out double value)
			=> TryGetValue(key, out value);

		public bool TryGetInt(string key, out int value)
			=> TryGetValue(key, out value);

		public bool TryGetLong(string key, out long value)
			=> TryGetValue(key, out value);

		public bool TryGetString(string key, [MaybeNullWhen(false)] out string value)
			=> TryGetValue(key, out value);

		public bool TryGetBool(string key, out bool value)
			=> TryGetValue(key, out value);

		public bool TryGetBytes(string key, [MaybeNullWhen(false)] out byte[] value)
			=> TryGetValue(key, out value);

		bool TryGetValue<T>(string key, [MaybeNullWhen(false)] out T? value)
		{
			if (_values.TryGetValue(key, out object? o) && o is T val)
			{
				value = val;
				return true;
			}
			if (_defaults.TryGetValue(key, out o) && o is T val2)
			{
				value = val2;
				return true;
			}
			value = default;
			return false;
		}

		public async Task LoadAsync(IUserOptionsRepository repository, CancellationToken cancellationToken)
		{
			IAsyncEnumerable<KeyValuePair<string, object>> dataLoader = repository.LoadAsync(cancellationToken);

			_values.Clear();
			await foreach (KeyValuePair<string, object> item in dataLoader)
				_values[item.Key] = item.Value;
		}
		#endregion IUserOptionsManager

		#region IUserOptions
		public IUserOptionsEdit GetEdit()
			=> new UserOptionsEdit(this);
		#endregion IUserOptions

		#region IUserOptionsEdit
		void SetValue(string key, object value)
		{
			if (_defaults.TryGetValue(key, out object? defVal) && value.Equals(defVal))
				ResetToDefault(key);
			else
				_values[key] = value;
		}

		void ResetToDefault(string key)
		{
			_values.Remove(key, out _);
		}

		void SignalChanged(IReadOnlyDictionary<string, object?> values)
		{
			Changed?.Invoke(null, values);
		}

		sealed class UserOptionsEdit : IUserOptionsEdit
		{
			readonly static object _resetToDefault = new object();
			readonly DefaultUserOptionsManager _manager;
			readonly Dictionary<string, object> _newValues = [];

			public UserOptionsEdit(DefaultUserOptionsManager manager)
			{
				_manager = manager;
			}

			public void SetValue(string key, string value)
				=> SetValueInt(key, value);

			public void SetValue(string key, int value)
				=> SetValueInt(key, value);

			public void SetValue(string key, long value)
				=> SetValueInt(key, value);

			public void SetValue(string key, double value)
				=> SetValueInt(key, value);

			public void SetValue(string key, bool value)
				=> SetValueInt(key, value);

			public void SetValue(string key, byte[] value)
				=> SetValueInt(key, value);

			public void ResetToDefault(string key)
				=> SetValueInt(key, _resetToDefault);

			void SetValueInt<T>(string key, T value) where T : notnull
				=> _newValues[key] = value;

			public void Apply()
			{
				foreach (KeyValuePair<string, object> newValue in _newValues)
				{
					if (newValue.Value == _resetToDefault)
						_manager.ResetToDefault(newValue.Key);
					else
						_manager.SetValue(newValue.Key, newValue.Value);
				}

				_manager.SignalChanged(_newValues);
			}

			public bool TryGetString(string key, out string? value)
				=> TryGetValue(key, out value);

			public bool TryGetInt(string key, out int value)
				=> TryGetValue(key, out value);

			public bool TryGetLong(string key, out long value)
				=> TryGetValue(key, out value);

			public bool TryGetDouble(string key, out double value)
				=> TryGetValue(key, out value);

			public bool TryGetBool(string key, out bool value)
				=> TryGetValue(key, out value);

			public bool TryGetBytes(string key, out byte[]? value)
				=> TryGetValue(key, out value);

			bool TryGetValue<T>(string key, [MaybeNullWhen(false)] out T? value)
			{
				if (_newValues.TryGetValue(key, out object? o) && o is T val)
				{
					value = val;
					return true;
				}
				return _manager.TryGetValue(key, out value);
			}
		}
		#endregion IUserOptionsEdit
	}
}

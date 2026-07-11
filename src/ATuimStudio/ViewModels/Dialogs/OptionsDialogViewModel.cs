using ATuimStudio.Extensions.Core;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ATuimStudio.ViewModels
{
	partial class OptionsDialogViewModel : ViewModelBase
	{
		[EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable CS8618
		public OptionsDialogViewModel()
		{ }
#pragma warning restore CS8618

		IList<OptionItem> _allOptions;
		public SupressableObservableCollection<OptionItem> Options { get; } = [];

		[ObservableProperty]
		string _searchText = "";

		readonly Window _dialogWindow;
		readonly IUserOptionsEdit _edit;
		public OptionsDialogViewModel(Window dialogWindow, IUserOptionsManager userOptionsManager)
		{
			_dialogWindow = dialogWindow;
			_edit = userOptionsManager.GetEdit();

			List<OptionItem> options = [.. _edit.GetAllOptions().Select(x => OptionItem.Create(_edit, x.Key, x.Value))];
			options.Sort();
			_allOptions = options;
			Options.AddRange(options);
		}

		[RelayCommand]
		void Ok()
		{
			_edit.Apply();
			_dialogWindow.Close();
		}

		[RelayCommand]
		void Cancel()
		{
			_dialogWindow.Close();
		}

		partial void OnSearchTextChanged(string value)
		{
			Options.SupressNotification();
			Options.Clear();
			Options.AddRange(value == ""
				? _allOptions
				: _allOptions.Where(x => x.Key.Contains(value, StringComparison.CurrentCultureIgnoreCase))
				);
			Options.RestoreNotification();
		}

		#region option classes
		public abstract partial class OptionItem : IComparable<OptionItem>
		{
			protected readonly IUserOptionsEdit _edit;
			protected OptionItem(IUserOptionsEdit edit, string key, object value)
			{
				_edit = edit;
				Key = key;
				Value = value;
			}

			public string Key { get; }
			public object Value { get; }

			public int CompareTo(OptionItem? other)
				=> Key.CompareTo(other?.Key);

			internal static OptionItem Create(IUserOptionsEdit edit, string key, object value)
			{
				if (value is string strVal)
					return new StringOptionItem(edit, key, strVal);
				if (value is int intVal)
					return new IntOptionItem(edit, key, intVal);
				if (value is long longVal)
					return new LongOptionItem(edit, key, longVal);
				if (value is double dblVal)
					return new DoubleOptionItem(edit, key, dblVal);
				if (value is bool boolVal)
					return new BoolOptionItem(edit, key, boolVal);
				if (value is byte[] bytesVal)
					return new BytesOptionItem(edit, key, bytesVal);
				throw new InvalidOperationException("Unsupported value type.");
			}

			[RelayCommand]
			void ClearOptionValue()
				=> _edit.ResetToDefault(Key);
		}

		public abstract class OptionItem<TValue> : OptionItem, IComparable<OptionItem> where TValue : notnull
		{
			protected TValue _value;
			internal OptionItem(IUserOptionsEdit edit, string key, TValue value) : base(edit, key, value)
			{
				_value = value;
			}
		}

		public sealed class StringOptionItem : OptionItem<string>
		{
			internal StringOptionItem(IUserOptionsEdit edit, string key, string value) : base(edit, key, value) { }
			public new string Value { get => _value; set { if (!_value.Equals(value)) { _value = value; _edit.SetValue(Key, value); } } }
		}
		public sealed class IntOptionItem : OptionItem<int>
		{
			internal IntOptionItem(IUserOptionsEdit edit, string key, int value) : base(edit, key, value) { }
			public new int Value { get => _value; set { if (!_value.Equals(value)) { _value = value; _edit.SetValue(Key, value); } } }
		}
		public sealed class LongOptionItem : OptionItem<long>
		{
			internal LongOptionItem(IUserOptionsEdit edit, string key, long value) : base(edit, key, value) { }
			public new long Value { get => _value; set { if (!_value.Equals(value)) { _value = value; _edit.SetValue(Key, value); } } }
		}
		public sealed class DoubleOptionItem : OptionItem<double>
		{
			internal DoubleOptionItem(IUserOptionsEdit edit, string key, double value) : base(edit, key, value) { }
			public new double Value { get => _value; set { if (!_value.Equals(value)) { _value = value; _edit.SetValue(Key, value); } } }
		}
		public sealed class BoolOptionItem : OptionItem<bool>
		{
			internal BoolOptionItem(IUserOptionsEdit edit, string key, bool value) : base(edit, key, value) { }
			public new bool Value { get => _value; set { if (!_value.Equals(value)) { _value = value; _edit.SetValue(Key, value); } } }
		}
		public sealed class BytesOptionItem : OptionItem<byte[]>
		{
			internal BytesOptionItem(IUserOptionsEdit edit, string key, byte[] value) : base(edit, key, value) { }
			public new byte[] Value { get => _value; set { if (!_value.SequenceEqual(value)) { _value = value; _edit.SetValue(Key, value); } } }
		}
		#endregion option classes
	}
}

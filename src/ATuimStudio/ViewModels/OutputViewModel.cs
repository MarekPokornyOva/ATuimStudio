using ATuimStudio.Extensions.Core;
using Dock.Model.Mvvm.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using AvaloniaEdit.Utils;
using Avalonia.Threading;

namespace ATuimStudio.ViewModels
{
	public sealed partial class OutputViewModel : Tool
	{
		public ObservableCollection<string> Types { get; } = [];
		[ObservableProperty]
		string? _selectedType;

		public ObservableCollection<OutputWriterLoggedEventArgs> Records { get; } = [];
		readonly Dictionary<string, List<OutputWriterLoggedEventArgs>> _allRecords = new Dictionary<string, List<OutputWriterLoggedEventArgs>>(StringComparer.Ordinal);

		[EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable CS8618
		public OutputViewModel()
		{
		}
#pragma warning restore CS8618

		readonly IOutputWriter _outputWriter;
		public OutputViewModel(IOutputWriter outputWriter) : this()
		{
			_outputWriter = outputWriter;

			_outputWriter.Logged += OutputWriter_Logged;
		}

		void OutputWriter_Logged(object? sender, OutputWriterLoggedEventArgs e)
		{
			Dispatcher.UIThread.Invoke(() => 
			{
				string type = e.Type;
				if (!_allRecords.TryGetValue(type, out List<OutputWriterLoggedEventArgs>? list))
					_allRecords.Add(type, list = new List<OutputWriterLoggedEventArgs>(32));
				list.Add(e);

				if (!Types.Any(x => x.EqualsOrdinal(type)))
				{
					Types.Add(type);
					if (Types.Count == 1)
					{
						SelectedType = type;
						return;
					}
				}

				if (e.Type.EqualsOrdinal(SelectedType))
					Records.Add(e);
			});			
		}

		partial void OnSelectedTypeChanged(string? value)
		{
			Records.Clear();
			if (value != null && _allRecords.TryGetValue(value, out List<OutputWriterLoggedEventArgs>? list))
				Records.AddRange(list);
		}
	}
}
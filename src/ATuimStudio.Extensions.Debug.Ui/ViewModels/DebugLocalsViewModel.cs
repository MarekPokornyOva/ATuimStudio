using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace ATuimStudio.Extensions.Debug;

public sealed class DebugLocalsViewModel : ObservableObject, IDisposable
{
	readonly ObservableCollection<VariableItem> _locals = [];
	public HierarchicalTreeDataGridSource<VariableItem> Source { get; }

	readonly IStackTraceProvider _stackTraceProvider;
	public DebugLocalsViewModel(IStackTraceProvider stackTraceProvider)
	{
		_stackTraceProvider = stackTraceProvider;
		stackTraceProvider.OnSelectedFrameChanged += SelectedFrameChanged;

		Source = new HierarchicalTreeDataGridSource<VariableItem>(_locals)
		{
			Columns =
				{
					new HierarchicalExpanderColumn<VariableItem>(
						new TextColumn<VariableItem, string>("Name", static x => x.Name),
						static x => x.Children,
						null,
						static x => x.IsExpanded
					),
					new TextColumn<VariableItem, string>("Value", static x => x.Value),
					new TextColumn<VariableItem, string>("Type", static x => x.TypeName),
				},
		};

		if (stackTraceProvider.SelectedFrame != null)
			ProcessSelectedFrameChanged();
	}

	public void Dispose()
	{
		_stackTraceProvider.OnSelectedFrameChanged -= SelectedFrameChanged;
	}

	static readonly VariableItem _dummyItem = new VariableItem("Expanding...", "", "", static x => { });
	void SelectedFrameChanged(object? sender, EventArgs e)
		=> ProcessSelectedFrameChanged();

	void ProcessSelectedFrameChanged()
	{
		VariableItem MapDebugItem(IDebugItem debugItem)
		{
			VariableItem res = new VariableItem(debugItem.Name, debugItem.Value, debugItem.TypeName, varItem =>
			{
				if (!debugItem.HasChildren)
					return;
				ObservableCollection<VariableItem> children = varItem.Children;
				if (children.Count == 1 && children[0] == _dummyItem)
				{
					children.Clear();
					FillCollection(debugItem.GetAllChildren(), children);
				}
			});
			if (debugItem.HasChildren)
				res.Children.Add(_dummyItem);
			return res;
		}

		void FillCollection(IEnumerable<IDebugItem> items, ObservableCollection<VariableItem> result)
		{
			result.AddRange(items.Select((x, i) =>
			{
				VariableItem res = MapDebugItem(x);
				void ValueChangedHandler(object? sender, EventArgs e)
				{
					x.ValueChanged -= ValueChangedHandler;
					result[i] = MapDebugItem(x);
				}
				x.ValueChanged += ValueChangedHandler;
				return res;
			}));
		}

		IStackFrame? stackFrame = _stackTraceProvider.SelectedFrame;
		Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
		{
			_locals.Clear();
			if (stackFrame != null)
				FillCollection(stackFrame.GetArguments().Union(stackFrame.GetLocals()), _locals);
		});
	}

	public class VariableItem
	{
		readonly Action<VariableItem> _expandChildren;
		public VariableItem(string name, string value, string typeName, Action<VariableItem> expandChildren)
		{
			Name = name;
			Value = value;
			TypeName = typeName;
			_expandChildren = expandChildren;
		}

		public string Name { get; }
		public string Value { get; }
		public string TypeName { get; }

		public ObservableCollection<VariableItem> Children { get; } = [];
		public bool IsExpanded { get; set { field = value; if (value) _expandChildren(this); } }
	};
}

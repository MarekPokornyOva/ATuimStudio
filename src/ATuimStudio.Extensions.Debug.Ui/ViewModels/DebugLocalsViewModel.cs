using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Dock.Model.Mvvm.Controls;
using System.Collections.ObjectModel;

namespace ATuimStudio.Extensions.Debug;

public sealed class DebugLocalsViewModel : Tool, IDisposable
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
	}

	public void Dispose()
	{
		_stackTraceProvider.OnSelectedFrameChanged -= SelectedFrameChanged;
	}

	static readonly VariableItem _dummyItem = new VariableItem("Expanding...", "", "", static x => { });
	void SelectedFrameChanged(object? sender, EventArgs e)
	{
		void FillCollection(IEnumerable<IDebugItem> items, ObservableCollection<VariableItem> result)
		{
			result.AddRange(items.Select(x =>
			{
				VariableItem res = new VariableItem(x.Name, x.Value, x.TypeName, varItem =>
				{
					if (!x.HasChildren)
						return;
					ObservableCollection<VariableItem> children = varItem.Children;
					if (children.Count == 1 && children[0] == _dummyItem)
					{
						children.Clear();
						FillCollection(x.GetAllChildren(), children);
					}
				});
				if (x.HasChildren)
					res.Children.Add(_dummyItem);
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
		public bool IsExpanded { get; set { field = value; _expandChildren(this); } }
	};
}

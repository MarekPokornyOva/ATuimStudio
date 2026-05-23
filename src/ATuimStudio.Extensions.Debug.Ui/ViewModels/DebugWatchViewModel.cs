using ATuimStudio.UiComponents;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ATuimStudio.Extensions.Debug;

public sealed class DebugWatchViewModel : ObservableObject, IDisposable
{
	readonly ObservableCollection<ExpressionItem> _expressionItems = [];
	public HierarchicalTreeDataGridSource<VariableItemBase> Source { get; }

	readonly IStackTraceProvider _stackTraceProvider;
	public DebugWatchViewModel(IStackTraceProvider stackTraceProvider)
	{
		_stackTraceProvider = stackTraceProvider;
		stackTraceProvider.OnSelectedFrameChanged += SelectedFrameChanged;

		Source = new HierarchicalTreeDataGridSource<VariableItemBase>(_expressionItems)
		{
			Columns =
				{
					new HierarchicalExpanderColumn<VariableItemBase>(
						new ConditionEditTemplateColumn<VariableItemBase>(static x => x is VariableItemBase vib && vib.CanEdit, "", "ExpressionCell", "ExpressionCellEdit", options: new TemplateColumnOptions<VariableItemBase>{ BeginEditGestures=BeginEditGestures.F2|BeginEditGestures.Tap }),
						static x => x.Children,
						null,
						static x => x.IsExpanded
					),
					new TextColumn<VariableItemBase, string>("Value", static x => x.Value),
					new TextColumn<VariableItemBase, string>("Type", static x => x.TypeName),
				},
		};

		NewExpressionItem();

		if (stackTraceProvider.SelectedFrame != null)
			ProcessSelectedFrameChanged();
	}

	public void Dispose()
	{
		_stackTraceProvider.OnSelectedFrameChanged -= SelectedFrameChanged;
	}

	static readonly DummyItem _dummyItem = new DummyItem();
	void SelectedFrameChanged(object? sender, EventArgs e)
		=> ProcessSelectedFrameChanged();

	void ProcessSelectedFrameChanged()
	{
		Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
		{
			foreach (ExpressionItem expressionItem in _expressionItems)
				expressionItem.Reevaluate();
		});
	}

	void NewExpressionItem()
	{
		ExpressionItem newItem = new ExpressionItem("", "", "", () => _stackTraceProvider.SelectedFrame);
		newItem.PropertyChanged += ItemPropertyChanged;
		_expressionItems.Add(newItem);
	}

	void ItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName != nameof(ExpressionItem.Expression) || sender is not ExpressionItem item)
			return;

		bool blankExists = _expressionItems.Any(x => x != item && x.Expression == "");
		if (item.Expression == "")
		{
			if (blankExists)
			{
				item.PropertyChanged -= ItemPropertyChanged;
				_expressionItems.Remove(item);
			}
		}
		else
		{
			if (!blankExists)
				NewExpressionItem();
		}
	}

	public abstract class VariableItemBase : ObservableObject
	{
		public VariableItemBase(string expression, string value, string typeName, bool canEdit)
		{
			_expression = expression;
			Value = value;
			TypeName = typeName;
			CanEdit = canEdit;
		}

		string _expression;
		public string Expression
		{
			get => _expression;
			set
			{
				if (_expression == value || !CanEdit)
					return;

				_expression = value;
				OnPropertyChanged(nameof(Expression));
				Reevaluate();
			}
		}
		public string Value { get; protected set { field = value; OnPropertyChanged(nameof(Value)); } }
		public string TypeName { get; protected set { field = value; OnPropertyChanged(nameof(TypeName)); } }
		public bool CanEdit { get; }

		internal virtual void Reevaluate() { }

		protected abstract void ExpandChildren();

		public ObservableCollection<VariableItemBase> Children { get; } = [];
		public bool IsExpanded { get; set { field = value; if (value) ExpandChildren(); } }
	}

	class DummyItem : VariableItemBase
	{
		public DummyItem() : base("", "", "", false)
		{
		}

		protected override void ExpandChildren()
		{}
	}

	public abstract class ObjectValueExpand : VariableItemBase
	{
		protected ObjectValueExpand(string expression, string value, string typeName, bool canEdit) : base(expression, value, typeName, canEdit)
		{
		}

		protected void ExpandChildren(IDebugItem objectValue)
		{
			ObservableCollection<VariableItemBase> children = this.Children;
			if (children.Count == 1 && children[0] == _dummyItem)
			{
				children.Clear();
				foreach (IDebugItem item in objectValue.GetAllChildren())
					children.Add(new ReadOnlyItem(item));
			}
		}
	}

	public class ExpressionItem : ObjectValueExpand
	{
		readonly Func<IStackFrame?> _frameProvider;
		public ExpressionItem(string expression, string value, string typeName, Func<IStackFrame?> frameProvider) : base(expression, value, typeName, true)
		{
			_frameProvider = frameProvider;
		}

		internal override void Reevaluate()
		{
			string expression = Expression;
			_objectValue = null;

			if (IsExpanded)
			{
				IsExpanded = false;
				OnPropertyChanged(nameof(IsExpanded));
			}

			if (expression == "")
			{
				Value = "";
				TypeName = "";
				Children.Clear();
			}
			else
			{
				IStackFrame? selectedFrame = _frameProvider();
				if (selectedFrame == null)
					return;
				IDebugItem objectValue = selectedFrame.Evaluate(expression);
				Value = objectValue.Value;
				TypeName = objectValue.TypeName;
				_objectValue = objectValue;

				if (Children.Count != 1 || Children[0] != _dummyItem)
				{
					Children.Clear();
					Children.Add(_dummyItem);
				}
			}
		}

		IDebugItem? _objectValue;

		protected override void ExpandChildren()
		{
			if (_objectValue == null)
				return;

			ExpandChildren(_objectValue);
		}
	};

	public class ReadOnlyItem : ObjectValueExpand
	{
		readonly IDebugItem _objectValue;
		public ReadOnlyItem(IDebugItem objectValue) : base(objectValue.Name, objectValue.Value, objectValue.TypeName, false)
		{
			_objectValue = objectValue;
			Children.Add(_dummyItem);
		}

		protected override void ExpandChildren()
			=> ExpandChildren(_objectValue);
	}
}

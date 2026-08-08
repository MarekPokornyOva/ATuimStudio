using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using System.Windows.Input;

namespace ATuimStudio.Common.Ui
{
	public class TableViewDoubleClickBehavior : Behavior<TableView>
	{
		public static readonly StyledProperty<ICommand> CommandProperty =
			 AvaloniaProperty.RegisterAttached<TableViewDoubleClickBehavior, TableView, ICommand>(
				  nameof(Command));

		public ICommand Command
		{
			get => GetValue(CommandProperty);
			set => SetValue(CommandProperty, value);
		}

		protected override void OnAttachedToVisualTree()
		{
			base.OnAttachedToVisualTree();
			AssociatedObject?.DoubleTapped += TableView_DoubleTapped;
		}

		protected override void OnDetachedFromVisualTree()
		{
			base.OnDetachedFromVisualTree();
			AssociatedObject?.DoubleTapped -= TableView_DoubleTapped;
		}

		private void TableView_DoubleTapped(object? sender, TappedEventArgs e)
		{
			TableView dataGrid = (TableView)sender!;
			if (dataGrid.SelectedItem != null && Command?.CanExecute(dataGrid.SelectedItem) == true)
				Command.Execute(dataGrid.SelectedItem);
		}
	}
}

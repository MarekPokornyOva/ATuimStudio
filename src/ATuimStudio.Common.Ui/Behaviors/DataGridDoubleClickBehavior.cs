using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using System.Windows.Input;

namespace ATuimStudio.Common.Ui
{
	public class DataGridDoubleClickBehavior : Behavior<DataGrid>
	{
		public static readonly StyledProperty<ICommand> CommandProperty =
			 AvaloniaProperty.RegisterAttached<DataGridDoubleClickBehavior, DataGrid, ICommand>(
				  nameof(Command));

		public ICommand Command
		{
			get => GetValue(CommandProperty);
			set => SetValue(CommandProperty, value);
		}

		protected override void OnAttachedToVisualTree()
		{
			base.OnAttachedToVisualTree();
			AssociatedObject?.DoubleTapped += DataGrid_DoubleTapped;
		}

		protected override void OnDetachedFromVisualTree()
		{
			base.OnDetachedFromVisualTree();
			AssociatedObject?.DoubleTapped -= DataGrid_DoubleTapped;
		}

		private void DataGrid_DoubleTapped(object? sender, TappedEventArgs e)
		{
			DataGrid dataGrid = (DataGrid)sender!;
			if (dataGrid.SelectedItem != null && Command?.CanExecute(dataGrid.SelectedItem) == true)
				Command.Execute(dataGrid.SelectedItem);
		}
	}
}

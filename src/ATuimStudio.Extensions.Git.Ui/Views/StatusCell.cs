using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ATuimStudio.Extensions.Git
{
	sealed class StatusCell : Control
	{
		public static readonly StyledProperty<GitRepositoryViewModel?> ViewModelProperty =
			 AvaloniaProperty.Register<StatusCell, GitRepositoryViewModel?>(nameof(ViewModel));
		public GitRepositoryViewModel? ViewModel
		{
			get => GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}
	
		public static readonly StyledProperty<int?> RowIndexProperty =
			 AvaloniaProperty.Register<StatusCell, int?>(nameof(RowIndex));
		public int? RowIndex
		{
			get => GetValue(RowIndexProperty);
			set => SetValue(RowIndexProperty, value);
		}

		readonly static Pen _pen = new Pen(new SolidColorBrush(Colors.Red));
		public override void Render(DrawingContext context)
		{
			base.Render(context);
	
			if (ViewModel is not { } viewModel || !RowIndex.HasValue)
				return;
	
			Rect bounds = this.Bounds;
			double height = bounds.Height;
			double height25 = height / 4;
			double width = bounds.Width;
			double widthHalf = width / 2;
	
			int rowIndex = RowIndex.Value;
			if (rowIndex < viewModel.IncommingCount)
			{
				//draw incomming
				Point bottomCenter = new Point(widthHalf, height - height25);
				double arrowTop = height - 2 * height25;
				context.DrawLine(_pen, new Point(widthHalf, height25), bottomCenter);
				context.DrawLine(_pen, new Point(1, arrowTop), bottomCenter);
				context.DrawLine(_pen, new Point(width - 2, arrowTop), bottomCenter);
			}
			else if (rowIndex < viewModel.IncommingCount + viewModel.OutgoingCount)
			{
				//draw outgoing
				Point topCenter = new Point(widthHalf, height25);
				double arrowTop = 2 * height25;
				context.DrawLine(_pen, topCenter, new Point(widthHalf, height - height25));
				context.DrawLine(_pen, new Point(1, arrowTop), topCenter);
				context.DrawLine(_pen, new Point(width - 2, arrowTop), topCenter);
			}
		}

		protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
		{
			base.OnPropertyChanged(change);

			if (change.Property == ViewModelProperty || change.Property == RowIndexProperty)
				InvalidateVisual();
		}
	}
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ATuimStudio.Extensions.Git.Models;

namespace ATuimStudio.Extensions.Git
{
	public partial class CommitGraphControl : UserControl
	{
		private CommitGraphData? _currentGraphData;

		public static readonly StyledProperty<CommitGraphData?> CommitGraphDataProperty =
			AvaloniaProperty.Register<CommitGraphControl, CommitGraphData?>(nameof(CommitGraphData), null);
		public CommitGraphData? CommitGraphData
		{
			get => GetValue(CommitGraphDataProperty);
			set => SetValue(CommitGraphDataProperty, value);
		}

		public static readonly StyledProperty<int> RowHeightProperty =
			AvaloniaProperty.Register<CommitGraphControl, int>(nameof(RowHeight));
		public int RowHeight
		{
			get => GetValue(RowHeightProperty);
			set => SetValue(RowHeightProperty, value);
		}

		public CommitGraphControl()
		{
			InitializeComponent();
			this.GetObservable(CommitGraphDataProperty).Subscribe(OnGraphDataChanged);
		}

		private void OnGraphDataChanged(CommitGraphData? graphData)
		{
			const int unitWidth = 12;
			int rowHeight = RowHeight;
			_currentGraphData = graphData;
			(double width, double height) = (0, 0);
			if (graphData != null)
				foreach (CommitGraphData.Dot dot in graphData.Dots)
				{
					Point point = dot.Center;
					if (width < point.X)
						width = point.X;
					if (height < point.Y)
						height = point.Y;
				}
			this.Width = width + unitWidth;
			this.Height = (height + .5) * rowHeight;

			//InvalidateVisual();
		}

		public override void Render(DrawingContext context)
		{
			base.Render(context);

			if (_currentGraphData is not { } graph)
				return;

			var startY = 0;
			var clipWidth = this.Width;
			var clipHeight = Bounds.Height;
			var rowHeight = RowHeight;
			var endY = startY + clipHeight + 28;

			bool onlyHighlightCurrentBranch = false;
			IBrush? dotBrush = null;
			using (context.PushClip(new Rect(0, 0, clipWidth, clipHeight)))
			using (context.PushTransform(Matrix.CreateTranslation(0, -startY)))
			{
				DrawCurves(context, graph, startY, endY, rowHeight, onlyHighlightCurrentBranch);
				DrawAnchors(context, graph, startY, endY, rowHeight, onlyHighlightCurrentBranch, dotBrush);
			}
		}

		private static void DrawCurves(DrawingContext context, Models.CommitGraphData graph, double top, double bottom, double rowHeight, bool onlyHighlightCurrentBranch)
		{
			var grayedPen = new Pen(new SolidColorBrush(Colors.Gray, 0.4), Models.CommitGraphData.Pens[0].Thickness);

			if (onlyHighlightCurrentBranch)
			{
				foreach (var link in graph.Links)
				{
					if (link.IsMerged)
						continue;

					var startY = link.Start.Y * rowHeight;
					var endY = link.End.Y * rowHeight;

					if (endY < top)
						continue;
					if (startY > bottom)
						break;

					var geo = new StreamGeometry();
					using (var ctx = geo.Open())
					{
						ctx.BeginFigure(new Point(link.Start.X, startY), false);
						ctx.QuadraticBezierTo(new Point(link.Control.X, link.Control.Y * rowHeight), new Point(link.End.X, endY));
					}

					context.DrawGeometry(null, grayedPen, geo);
				}
			}

			foreach (var line in graph.Paths)
			{
				var last = new Point(line.Points[0].X, line.Points[0].Y * rowHeight);
				var size = line.Points.Count;
				var endY = line.Points[size - 1].Y * rowHeight;

				if (endY < top)
					continue;
				if (last.Y > bottom)
					break;

				var geo = new StreamGeometry();
				var pen = Models.CommitGraphData.Pens[line.Color];

				using (var ctx = geo.Open())
				{
					var started = false;
					var ended = false;
					for (int i = 1; i < size; i++)
					{
						var cur = new Point(line.Points[i].X, line.Points[i].Y * rowHeight);
						if (cur.Y < top)
						{
							last = cur;
							continue;
						}

						if (!started)
						{
							ctx.BeginFigure(last, false);
							started = true;
						}

						if (cur.Y > bottom)
						{
							cur = new Point(cur.X, bottom);
							ended = true;
						}

						if (cur.X > last.X)
						{
							ctx.QuadraticBezierTo(new Point(cur.X, last.Y), cur);
						}
						else if (cur.X < last.X)
						{
							if (i < size - 1)
							{
								var midY = (last.Y + cur.Y) / 2;
								ctx.CubicBezierTo(new Point(last.X, midY + 4), new Point(cur.X, midY - 4), cur);
							}
							else
							{
								ctx.QuadraticBezierTo(new Point(last.X, cur.Y), cur);
							}
						}
						else
						{
							ctx.LineTo(cur);
						}

						if (ended)
							break;
						last = cur;
					}
				}

				if (!line.IsMerged && onlyHighlightCurrentBranch)
					context.DrawGeometry(null, grayedPen, geo);
				else
					context.DrawGeometry(null, pen, geo);
			}

			foreach (var link in graph.Links)
			{
				if (onlyHighlightCurrentBranch && !link.IsMerged)
					continue;

				var startY = link.Start.Y * rowHeight;
				var endY = link.End.Y * rowHeight;

				if (endY < top)
					continue;
				if (startY > bottom)
					break;

				var geo = new StreamGeometry();
				using (var ctx = geo.Open())
				{
					ctx.BeginFigure(new Point(link.Start.X, startY), false);
					ctx.QuadraticBezierTo(new Point(link.Control.X, link.Control.Y * rowHeight), new Point(link.End.X, endY));
				}

				context.DrawGeometry(null, Models.CommitGraphData.Pens[link.Color], geo);
			}
		}

		private static void DrawAnchors(DrawingContext context, Models.CommitGraphData graph, double top, double bottom, double rowHeight, bool onlyHighlightCurrentBranch, IBrush? dotBrush)
		{
			var dotFill = dotBrush;
			var dotFillPen = new Pen(dotFill, 2);
			var grayedPen = new Pen(Brushes.Gray, Models.CommitGraphData.Pens[0].Thickness);

			foreach (var dot in graph.Dots)
			{
				var center = new Point(dot.Center.X, dot.Center.Y * rowHeight);

				if (center.Y < top)
					continue;
				if (center.Y > bottom)
					break;

				var pen = Models.CommitGraphData.Pens[dot.Color];
				if (!dot.IsMerged && onlyHighlightCurrentBranch)
					pen = grayedPen;

				switch (dot.Type)
				{
					case Models.CommitGraphData.DotType.Head:
					context.DrawEllipse(dotFill, pen, center, 6, 6);
					context.DrawEllipse(pen.Brush, null, center, 3, 3);
					break;
					case Models.CommitGraphData.DotType.Merge:
					context.DrawEllipse(pen.Brush, null, center, 6, 6);
					context.DrawLine(dotFillPen, new Point(center.X, center.Y - 3), new Point(center.X, center.Y + 3));
					context.DrawLine(dotFillPen, new Point(center.X - 3, center.Y), new Point(center.X + 3, center.Y));
					break;
					default:
					context.DrawEllipse(dotFill, pen, center, 3, 3);
					break;
				}
			}
		}
	}
}


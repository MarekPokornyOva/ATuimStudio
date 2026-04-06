using ATuimStudio.Extensions.Debug;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;

namespace ATuimStudio.UiComponents
{
	//https://github.com/avaloniaui/avaloniaedit/blob/4290c429/src/AvaloniaEdit.Demo/CustomMargin.cs#L19-L142
	public sealed class BreakpointMargin : AbstractMargin, IDisposable
	{
		//Move this to Theme
		private readonly static IBrush _backgroundBrush = new ImmutableSolidColorBrush(new Color(255, 51, 51, 51));
		private readonly static IBrush _pointerOverBrush = new ImmutableSolidColorBrush(new Color(192, 80, 80, 80));
		private readonly static IPen _pointerOverPen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(192, 37, 37, 37)), 1);
		private readonly static IBrush _markerBrush = new ImmutableSolidColorBrush(new Color(255, 195, 81, 92));
		private readonly static IPen _markerPen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(255, 240, 92, 104)), 1);
		readonly static Cursor _cursor = new Cursor(StandardCursorType.Hand);

		private int _pointerOverLine = -1;

		readonly IBreakpointManager _breakpointManager;
		public BreakpointMargin(IBreakpointManager breakpointManager)
		{
			_breakpointManager = breakpointManager;
			Cursor = _cursor;

			_breakpointManager.BreakpointAdded += BreakpointToggled;
			_breakpointManager.BreakpointRemoved += BreakpointToggled;
		}

		void BreakpointToggled(object? sender, Breakpoint e)
			=> InvalidateVisual();

		protected override void OnTextViewChanged(TextView? oldTextView, TextView? newTextView)
		{
			if (oldTextView != null)
			{
				oldTextView.VisualLinesChanged -= OnVisualLinesChanged;
				oldTextView.DocumentChanged -= OnDocumentChanged;
			}

			if (newTextView != null)
			{
				newTextView.VisualLinesChanged += OnVisualLinesChanged;
				newTextView.DocumentChanged += OnDocumentChanged;
			}

			base.OnTextViewChanged(oldTextView, newTextView);
		}

		private void OnVisualLinesChanged(object? sender, EventArgs eventArgs)
		{
			InvalidateVisual();
		}

		private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
		{
			InvalidateVisual();
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			return new Size(20, 0);
		}

		private (int Line, int Column) GetLineNumber(PointerEventArgs e)
		{
			double visualY = e.GetPosition(TextView).Y + TextView.VerticalOffset;
			VisualLine? visualLine = TextView.GetVisualLineFromVisualTop(visualY);
			if (visualLine == null)
				return (-1, -1);
			int lineNumber = visualLine.FirstDocumentLine.LineNumber;
			var caret = TextArea.Caret;
			return lineNumber == caret.Line
				? (lineNumber, caret.Column)
				: (lineNumber, 1);
		}

		protected override void OnPointerMoved(PointerEventArgs e)
		{
			_pointerOverLine = GetLineNumber(e).Line;
			InvalidateVisual();

			base.OnPointerMoved(e);
		}

		protected override void OnPointerExited(PointerEventArgs e)
		{
			_pointerOverLine = -1;
			InvalidateVisual();

			base.OnPointerExited(e);
		}

		protected override void OnPointerPressed(PointerPressedEventArgs e)
		{
			(int line, int column) = GetLineNumber(e);
			_pointerOverLine = line;

			_breakpointManager.ToggleBreakpoint(Document.FileName, line, column);

			//InvalidateVisual();
			e.Handled = true;

			base.OnPointerPressed(e);
		}

		public override void Render(DrawingContext context)
		{
			context.DrawRectangle(_backgroundBrush, null, Bounds);

			if (TextView?.VisualLinesValid == true)
			{
				foreach (var visualLine in TextView.VisualLines)
				{
					double y = visualLine.VisualTop - TextView.VerticalOffset + visualLine.Height / 2;

					int line = visualLine.FirstDocumentLine.LineNumber;
					string filepath = Document.FileName;
					if (_breakpointManager.Breakpoints.Any(bp => bp.Filepath.EqualsOrdinal(filepath) && bp.Range.Start.Line == line))
						context.DrawEllipse(_markerBrush, _markerPen, new Point(10, y), 8, 8);
					else if (_pointerOverLine == line)
						context.DrawEllipse(_pointerOverBrush, _pointerOverPen, new Point(10, y), 8, 8);
				}
			}

			base.Render(context);
		}

		public void Dispose()
		{
			_breakpointManager.BreakpointAdded -= BreakpointToggled;
			_breakpointManager.BreakpointRemoved -= BreakpointToggled;
		}
	}
}

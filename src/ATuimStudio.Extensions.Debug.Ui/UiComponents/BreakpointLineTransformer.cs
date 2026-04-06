using ATuimStudio.Extensions.Debug;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace ATuimStudio.UiComponents
{
	sealed class BreakpointLineTransformer : DocumentColorizingTransformer, IDisposable
	{
		readonly TextView _textView;
		readonly IBreakpointManager _manager;
		readonly IStackTraceProvider _stackTraceProvider;
		SourceRange? _actualBreakpoint;
		public BreakpointLineTransformer(TextView textView, IBreakpointManager manager, IStackTraceProvider stackTraceProvider)
		{
			_textView = textView;
			_manager = manager;
			_stackTraceProvider = stackTraceProvider;

			manager.BreakpointAdded += BreakpointToggled;
			manager.BreakpointRemoved += BreakpointToggled;
			stackTraceProvider.OnCallStackChanged += CallStackChanged;
		}

		public void Dispose()
		{
			_manager.BreakpointAdded -= BreakpointToggled;
			_manager.BreakpointRemoved -= BreakpointToggled;
			_stackTraceProvider.OnCallStackChanged += CallStackChanged;
		}

		void BreakpointToggled(object? sender, Breakpoint e)
		{
			TextDocument? doc = _textView.Document;
			if (doc != null && e.Filepath.EqualsOrdinal(doc.FileName))
				Redraw(e.Range, doc);
		}

		void Redraw(SourceRange range, TextDocument doc)
		{
			int GetOffset(SourcePosition pos)
				=> doc.GetOffset(pos.Line, pos.Column);
			int startpos = GetOffset(range.Start);
			_textView.Redraw(new SimpleSegment(startpos, GetOffset(range.End) - startpos));
		}

		void CallStackChanged(object? sender, EventArgs e)
			=> Dispatcher.UIThread.Invoke(UiCallStackChanged);

		void UiCallStackChanged()
		{
			IReadOnlyList<IStackFrame>? callStack = _stackTraceProvider.CallStack;
			TextDocument? doc = _textView.Document;
			if (doc == null)
				return;

			if (callStack == null || callStack.Count == 0)
			{
				if (_actualBreakpoint.HasValue)
				{
					SourceRange range = _actualBreakpoint.Value;
					_actualBreakpoint = null;
					Redraw(range, doc);
				}
			}
			else
			{
				IStackFrame frame = callStack[0];
				SourceRange? frameRange = frame.Range;
				if (!frame.SourceFilePath.EqualsOrdinal(doc.FileName) || !frameRange.HasValue)
					return;

				SourceRange sourceRange = frameRange.Value;
				_actualBreakpoint = sourceRange;
				Redraw(sourceRange, doc);
			}
		}

		//Move this to Theme
		private readonly static IBrush _markerBrush = new ImmutableSolidColorBrush(new Color(255, 195, 81, 92));
		private readonly static IBrush _actualMarkerBrush = new ImmutableSolidColorBrush(Color.FromRgb(205, 192, 94));
		protected override void ColorizeLine(DocumentLine line)
		{
			string lineText = this.CurrentContext.Document.GetText(line);
			int textStart = -1;
			int a = 0;
			foreach (char ch in lineText)
			{
				if (!char.IsWhiteSpace(ch))
				{
					textStart = a;
					break;
				}
				a++;
			}
			if (textStart == -1)
				return;

			string filePath = this.CurrentContext.Document.FileName;
			ChangeLinePart(line.Offset + textStart, line.Offset + line.Length - (textStart == 0 ? textStart : textStart - 1),
				visualLine =>
				{
					if (_actualBreakpoint.HasValue && _actualBreakpoint.Value.Start.Line == line.LineNumber)
						visualLine.BackgroundBrush = _actualMarkerBrush;
					else if (_manager.Breakpoints.Any(bp => bp.Filepath.EqualsOrdinal(filePath) && bp.Range.Start.Line == line.LineNumber))
						visualLine.BackgroundBrush = _markerBrush;
				});
		}
	}
}

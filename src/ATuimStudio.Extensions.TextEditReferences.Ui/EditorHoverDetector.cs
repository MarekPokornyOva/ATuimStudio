using Avalonia;
using Avalonia.Input;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;

namespace ATuimStudio.Extensions.TextEditReferences
{
	public sealed class EditorHoverDetector
	{
		private readonly TextArea _textArea;
		private readonly DispatcherTimer _hoverTimer;
		private readonly double _hoverSensitivityDistance;
		private Point _lastMousePosition;
		private TextViewPosition? _hoverPosition;

		public event EventHandler<TextViewPosition>? OnHoverDetected;
		public event EventHandler? OnHoverEnded;

		public EditorHoverDetector(TextArea textArea, int hoverDelayMs, double hoverSensitivityDistance)
		{
			_textArea = textArea;
			_hoverTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(hoverDelayMs)
			};
			_hoverTimer.Tick += HoverTimer_Tick;
			_hoverSensitivityDistance = hoverSensitivityDistance;

			_textArea.PointerEntered += Editor_MouseEntered;
			_textArea.PointerMoved += Editor_MouseMove;
			_textArea.PointerExited += Editor_MouseLeave;
		}

		private void Editor_MouseEntered(object? sender, PointerEventArgs e)
		{
			_hoverTimer.Start();
		}

		private void Editor_MouseMove(object? sender, PointerEventArgs e)
		{
			TextView textView = _textArea.TextView;
			Point currentPosition = e.GetPosition(textView) + textView.ScrollOffset;

			// If mouse moved significantly, restart timer
			if ((currentPosition - _lastMousePosition).Length > _hoverSensitivityDistance)
			{
				_hoverTimer.Stop();
				_lastMousePosition = currentPosition;
				_hoverTimer.Start();
				_hoverPosition = null;
			}
		}

		private void HoverTimer_Tick(object? sender, EventArgs e)
		{
			_hoverTimer.Stop();

			// Get the text position at cursor
			TextViewPosition? position = _textArea.TextView.GetPositionFloor(_lastMousePosition);
			if (position.HasValue)
			{
				_hoverPosition = position.Value;
				OnHoverDetected?.Invoke(this, position.Value);
			}
		}

		private void Editor_MouseLeave(object? sender, PointerEventArgs e)
		{
			_hoverTimer.Stop();
			if (_hoverPosition.HasValue)
			{
				OnHoverEnded?.Invoke(this, EventArgs.Empty);
				_hoverPosition = null;
			}
		}
	}

}

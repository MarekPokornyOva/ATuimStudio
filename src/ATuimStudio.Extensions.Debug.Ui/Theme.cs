using Avalonia.Media;

namespace ATuimStudio
{
	static class Theme
	{
		internal static ITheme Current { get; } = new DarkTheme();
	}

	interface ITheme
	{
		IBrush BreakPoint { get; }
		IBrush BreakPointCurrent { get; }
	}

	class DarkTheme : ITheme
	{
		internal readonly static IBrush _breakPoint;
		public IBrush BreakPoint => _breakPoint;
		internal readonly static IBrush _breakPointCurrent;
		public IBrush BreakPointCurrent => _breakPointCurrent;

		static DarkTheme()
		{
			_breakPoint = new SolidColorBrush(Color.FromRgb(131, 66, 75));
			_breakPointCurrent = new SolidColorBrush(Color.FromRgb(205, 192, 94));
		}
	}
}

using Avalonia;
using Avalonia.Media;
using Avalonia.Themes.Fluent;

namespace ATuimStudio
{
	static class Theme
	{
		internal static ITheme Current { get; } = new DarkTheme();
	}

	interface ITheme
	{
		IBrush EditorBackground { get; }
		IBrush ControlHigh { get; }
	}

	class DarkTheme : ITheme
	{
		internal readonly static IBrush _editorBackground;
		public IBrush EditorBackground => _editorBackground;

		internal readonly static IBrush _controlHigh;
		public IBrush ControlHigh => _controlHigh;

		static DarkTheme()
		{
			Application? app = Application.Current;
			FluentTheme theme = app?.Styles.OfType<FluentTheme>().FirstOrDefault()
				?? new FluentTheme();

			IBrush GetBrush(string key)
				=> theme.TryGetResource(key, app?.RequestedThemeVariant, out object? resource) && resource is IBrush brush
					? brush
					: throw new InvalidOperationException($"No {key} found in theme.");

			_editorBackground = GetBrush("SystemControlBackgroundBaseLowBrush");
			_controlHigh = GetBrush("SystemControlHighlightBaseLowBrush");
		}
	}
}

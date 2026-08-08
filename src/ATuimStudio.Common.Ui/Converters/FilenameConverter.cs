using Avalonia.Data.Converters;
using System.Globalization;

namespace ATuimStudio
{
	public sealed class FilenameConverter : IValueConverter
	{
		public static FilenameConverter Instance { get; } = new FilenameConverter();

		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value is string valStr
				? Path.GetFileName(valStr)
				: value;

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}

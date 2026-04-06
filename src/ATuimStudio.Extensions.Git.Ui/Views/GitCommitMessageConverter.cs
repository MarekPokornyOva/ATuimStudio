using Avalonia.Data.Converters;
using System.Globalization;

namespace ATuimStudio.Extensions.Git
{
	public sealed class GitCommitMessageConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value is string valStr
				? valStr.Trim().Replace('\n', ' ')
				: value;

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}
}

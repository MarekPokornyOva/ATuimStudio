using Avalonia.Data.Converters;
using System.Globalization;

namespace ATuimStudio.Extensions.Git
{
	public sealed class GitCommitTimeConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value is DateTimeOffset valDt
				? valDt.ToString("G")
				: value;

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}
}

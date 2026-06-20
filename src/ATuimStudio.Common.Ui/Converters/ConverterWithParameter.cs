using Avalonia.Data.Converters;
using System.Globalization;

namespace ATuimStudio
{
	public class ConverterWithParameter : IValueConverter
	{
		public IValueConverter? Converter { get; set; }
		public object? Parameter { get; set; }

		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> Converter?.Convert(value, targetType, Parameter, culture);

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}

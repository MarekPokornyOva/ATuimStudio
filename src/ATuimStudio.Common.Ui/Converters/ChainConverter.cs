using Avalonia.Data.Converters;
using System.Globalization;

namespace ATuimStudio
{
	public sealed class ChainConverter : IValueConverter
	{
		public IList<IValueConverter> Children { get; } = new List<IValueConverter>();

		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			foreach (IValueConverter converter in Children)
				value = converter.Convert(value, targetType, parameter, culture);
			return value;
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}

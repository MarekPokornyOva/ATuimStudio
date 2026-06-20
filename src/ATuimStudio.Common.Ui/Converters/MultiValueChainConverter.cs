using Avalonia;
using Avalonia.Data.Converters;
using System.Globalization;

namespace ATuimStudio
{
	public sealed class MultiValueChainConverter : IMultiValueConverter
	{
		public IMultiValueConverter? Entry { get; set; }
		public IValueConverter? Other { get; set; }

		public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
		{
			object? result = Entry?.Convert(values, targetType, parameter, culture);
			if (result == null || result == AvaloniaProperty.UnsetValue)
				return result;

			if (Other != null)
				result = Other.Convert(result, targetType, parameter, culture);

			return result;
		}
	}
}

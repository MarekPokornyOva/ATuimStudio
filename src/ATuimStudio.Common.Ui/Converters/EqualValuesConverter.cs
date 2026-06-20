using Avalonia;
using Avalonia.Data.Converters;
using System.Globalization;

namespace ATuimStudio
{
	public sealed class EqualValuesConverter : IMultiValueConverter
	{
		public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
			=> values.Count switch
			{
				0 => AvaloniaProperty.UnsetValue,
				1 => true,
				2 => object.Equals(values[0], values[1]),
				_ => Equal(values, 0)
			};

		static bool Equal(IList<object?> values, int index)
		{
			int index1 = index + 1;
			return object.Equals(values[index], values[index1]) && (index1 + 1 == values.Count || Equal(values, index1));
		}
	}
}

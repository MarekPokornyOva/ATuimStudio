using Avalonia.Data.Converters;
using System.Globalization;

namespace ATuimStudio.Views
{
	public sealed class BytesStringConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value is byte[] bytes
				//? string.Join(' ', bytes.Select(static x => x.ToString("x2")))
				? BitConverter.ToString(bytes).Replace("-", " ")
				: value;

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value is string strVal
				? System.Convert.FromHexString(strVal.Replace(" ", ""))
				: value;
	}
}

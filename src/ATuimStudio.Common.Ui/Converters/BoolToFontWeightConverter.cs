using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

namespace ATuimStudio
{

	public sealed class BoolToFontWeightConverter : IValueConverter
	{
		public FontWeight Default { get; set; } = FontWeight.Normal;

		static readonly char[] _delimiters = [',', ';'];
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is bool isTrue)
			{
				if (parameter is BoolToFontWeightConverterParams classParams)
					return isTrue ? classParams.True : classParams.False;

				if (parameter is string strParams)
				{
					int pos = strParams.IndexOfAny(_delimiters);
					return Enum.Parse<FontWeight>(isTrue ? strParams[..pos] : strParams[(pos + 1)..]);
				}
			}

			return Default;
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public sealed class BoolToFontWeightConverterParams
	{
		public FontWeight True { get; set; }
		public FontWeight False { get; set; }
	}
}
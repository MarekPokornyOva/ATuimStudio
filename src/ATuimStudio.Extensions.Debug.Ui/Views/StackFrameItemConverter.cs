using Avalonia.Data.Converters;
using System.Globalization;

namespace ATuimStudio.Extensions.Debug
{
	public sealed class StackFrameItemConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value is IStackFrame stackFrame
				? stackFrame.FullStackFrameText
				: value;

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}
}

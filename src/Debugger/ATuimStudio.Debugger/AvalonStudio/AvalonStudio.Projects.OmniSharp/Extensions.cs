#nullable disable

namespace AvalonStudio.Debugging.DotNetCore
{
	using System.Reflection.Metadata;

	public static class Extensions
	{
		public static bool IsWithin(this SequencePoint point, int line, int column)
		{
			if (point.StartLine == line)
			{
				if (0 < column && point.StartColumn > column)
				{
					return false;
				}
			}

			if (point.EndLine == line)
			{
				if (point.EndColumn < column)
				{
					return false;
				}
			}

			if (!((point.StartLine <= line) && (point.EndLine >= line)))
			{
				return false;
			}

			return true;
		}

		public static bool IsWithinLineOnly(this SequencePoint point, int line)
		{
			return point.StartLine <= line && line <= point.EndLine;
		}

		public static bool IsGreaterThan(this SequencePoint point, int line, int column)
		{
			return (point.StartLine > line) || (point.StartLine == line && point.StartColumn > column);
		}

		public static bool IsLessThan(this SequencePoint point, int line, int column)
		{
			return (point.StartLine < line) || (point.StartLine == line && point.StartColumn < column);
		}

		public static bool IsUserLine(this SequencePoint point)
		{
			return point.StartLine != 0xfeefee;
		}

		public static int LineRange(this SequencePoint point)
		{
			return point.EndLine - point.StartLine;
		}
	}
}
using System.Runtime.CompilerServices;

namespace System
{
	public static class StringExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool EqualsOrdinal(this string? x, string? y)
			=> string.Equals(x, y, StringComparison.Ordinal);
	}
}

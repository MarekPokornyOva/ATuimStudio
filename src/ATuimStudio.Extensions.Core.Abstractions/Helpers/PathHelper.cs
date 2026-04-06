namespace ATuimStudio.Extensions.Core
{
	public static class PathHelper
	{
		public static readonly IEqualityComparer<string> PathEqualityComparer = Environment.OSVersion.Platform == PlatformID.Win32NT ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
		public static readonly IComparer<string> PathComparer = Environment.OSVersion.Platform == PlatformID.Win32NT ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
		public static readonly StringComparison PathStringComparison = Environment.OSVersion.Platform == PlatformID.Win32NT ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

		public static bool IsRootPathOf(string parent, string child)
		{
			int parLen = parent.Length;
			char chr;
			return child.Length >= parLen
				&& string.Compare(parent, 0, child, 0, parLen, PathStringComparison) == 0
				&& (child.Length == parLen || ((chr = child[parLen]) == Path.DirectorySeparatorChar || chr == Path.AltDirectorySeparatorChar));
		}
	}
}

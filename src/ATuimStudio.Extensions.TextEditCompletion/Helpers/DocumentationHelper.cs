using System.Text.RegularExpressions;

namespace ATuimStudio.Extensions.TextEditCompletion
{
	static partial class DocumentationHelper
	{
		/// <summary>
		/// Cleans up XML text by removing extra whitespace and line breaks.
		/// </summary>
		internal static string CleanXmlText(string text)
			=> string.IsNullOrEmpty(text)
				? string.Empty
				: SpacesRegex().Replace(text, " ").Trim(); // Remove extra whitespace and normalize line breaks

		[GeneratedRegex(@"\s+")]
		private static partial Regex SpacesRegex();
	}
}

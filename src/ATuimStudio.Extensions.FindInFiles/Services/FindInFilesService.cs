using ATuimStudio.Extensions.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace ATuimStudio.Extensions.FindInFiles
{
	partial class FindInFilesService : IFindInFiles
	{
		readonly ISolutionService _solutionService;
		public FindInFilesService(ISolutionService solutionService)
		{
			_solutionService = solutionService;
		}

		public IAsyncEnumerable<TextSearchResult> FindTextAsync(string searchText, bool useRegex, CancellationToken cancellationToken)
			=> FindTextInternalAsync(searchText, useRegex, cancellationToken);

		public async IAsyncEnumerable<TextSearchResult> ReplaceTextAsync(string searchText, bool useRegex, string replaceText, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
			await foreach (TextSearchResult item in FindTextInternalAsync(searchText, useRegex, cancellationToken))
			{
				_solutionService.UpdateDocumentContent(item.FilePath, new DocumentUpdateInfo(item.Offset, replaceText, searchText));
				yield return item;
			}
		}

		async IAsyncEnumerable<TextSearchResult> FindTextInternalAsync(string searchText, bool useRegex, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
			if (_solutionService.CurrentSolution?.RawData is not Solution slnData)
				yield break;

			Regex? regex = null;

			if (useRegex)
			{
				try
				{
					regex = new Regex(searchText, RegexOptions.IgnoreCase);
				}
				catch (ArgumentException ex)
				{
					throw new InvalidOperationException($"Invalid regex pattern: {ex.Message}");
				}
			}

			foreach (Project project in slnData.Projects)
				foreach (Document document in project.Documents)
				{
					SourceText text = await document.GetTextAsync(cancellationToken);
					for (int i = 0; i < text.Lines.Count; i++)
					{
						TextLine line = text.Lines[i];
						string lineText = line.ToString();

						if (useRegex)
						{
							MatchCollection matches = regex!.Matches(lineText);
							foreach (Match match in matches)
								yield return new TextSearchResult
									(
										document.FilePath!,
										lineText,
										i + 1,
										match.Index + 1,
										line.Start + match.Index,
										match.Length
									);
						}
						else
						{
							int index = 0;
							while ((index = lineText.IndexOf(searchText, index, StringComparison.OrdinalIgnoreCase)) != -1)
							{
								yield return new TextSearchResult
									(
										document.FilePath!,
										lineText,
										i + 1,
										index + 1,
										line.Start + index,
										searchText.Length
									);
								index += searchText.Length;
							}
						}
					}
				}
		}
	}
}
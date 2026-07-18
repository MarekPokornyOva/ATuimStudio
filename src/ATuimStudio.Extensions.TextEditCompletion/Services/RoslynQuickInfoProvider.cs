using ATuimStudio.Extensions.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.QuickInfo;

namespace ATuimStudio.Extensions.TextEditCompletion
{
	sealed class RoslynQuickInfoProvider : IQuickInfoProvider
	{
		readonly IDocumentService _documentService;
		public RoslynQuickInfoProvider(IDocumentService documentService)
		{
			_documentService = documentService;
		}

		static readonly IQuickInfoResult? _emptyResult = null;
		public async Task<IQuickInfoResult?> Get(string path, int position, CancellationToken cancellationToken)
		{
			Document? document = _documentService.GetDocument(path);
			if (document == null)
				return _emptyResult;

			QuickInfoService? quickInfoService = QuickInfoService.GetService(document);
			if (quickInfoService == null)
				return _emptyResult;

			QuickInfoItem? quickInfoItem = await quickInfoService.GetQuickInfoAsync(document, position, cancellationToken);
			if (quickInfoItem == null)
				return _emptyResult;

			return new QuickInfoResult(quickInfoItem);
		}

		sealed class QuickInfoResult : IQuickInfoResult
		{
			readonly QuickInfoItem _quickInfoItem;

			public QuickInfoResult(QuickInfoItem quickInfoItem)
			{
				_quickInfoItem = quickInfoItem;
			}
		}
	}
}

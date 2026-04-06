using ATuimStudio.Extensions.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Recommendations;
using System.Collections.Immutable;

namespace ATuimStudio.Extensions.TextEditCompletion
{
	class RecommenderCompletionProvider : ITextEditCompletionProvider
	{
		readonly IDocumentService _documentService;
		public RecommenderCompletionProvider(IDocumentService documentService)
		{
			_documentService = documentService;
		}

		public async Task<IReadOnlyCollection<ITextEditCompletionItem>> GetCompletions(string path, int position, CancellationToken cancellationToken)
		{
			Document? document = _documentService.GetDocument(path);
			if (document == null)
				return [];

			ImmutableArray<ISymbol> symbols = await Recommender.GetRecommendedSymbolsAtPositionAsync(document, position, cancellationToken: cancellationToken);
			return [.. symbols.GroupBy(static x => x.Name).Select(static x => new TextEditCompletionItem(x.Key, [.. x])).OrderBy(static x => x.Text)];
		}

		class TextEditCompletionItem : ITextEditCompletionItem
		{
			readonly string _name;
			readonly ISymbol[] _symbols;
			IReadOnlyCollection<ICodeEditCompletionItem>? _codeItems;
			internal TextEditCompletionItem(string name, ISymbol[] symbols)
			{
				_name = name;
				_symbols = symbols;
				Label = symbols[0] is IMethodSymbol
					? name + "(...)"
					: name;
			}

			public string Text => _name;

			public string Label { get; }

			public double Priority => 0;

			public IReadOnlyCollection<ICodeEditCompletionItem> CodeItems => _codeItems ??= new MappingReadOnlyCollection<ISymbol, ICodeEditCompletionItem>(_symbols, static x => new CodeEditCompletionItem(x));

			class CodeEditCompletionItem : ICodeEditCompletionItem
			{
				readonly ISymbol _symbol;
				internal CodeEditCompletionItem(ISymbol symbol)
					=> _symbol = symbol;

				public string Text => _symbol.ToString() ?? _symbol.Name;

				public CodeEditCompletionItemType Type => _symbol is IMethodSymbol ? CodeEditCompletionItemType.Method : CodeEditCompletionItemType.Other;
			}
		}
	}
}

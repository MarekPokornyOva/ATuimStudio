using ATuimStudio.Extensions.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Recommendations;
using System.Xml.Linq;

namespace ATuimStudio.Extensions.TextEditCompletion
{
	sealed class RecommenderCompletionProvider : ITextEditCompletionProvider
	{
		readonly IDocumentService _documentService;
		public RecommenderCompletionProvider(IDocumentService documentService)
		{
			_documentService = documentService;
		}

		static readonly TextEditCompletionResult _emptyResult = new TextEditCompletionResult([], null);
		public async Task<ITextEditCompletionResult> GetCompletionsAsync(string path, int position, CancellationToken cancellationToken)
		{
			Document? document = _documentService.GetDocument(path);
			if (document == null)
				return _emptyResult;

			//CompletionService? service = CompletionService.GetService(document);
			//if (service == null)
			//	return _emptyResult;
			//CompletionList data = await service.GetCompletionsAsync(document, position, cancellationToken: cancellationToken);

			IEnumerable<ISymbol> symbols = await Recommender.GetRecommendedSymbolsAtPositionAsync(document, position, cancellationToken: cancellationToken);

			TextEditCompletionIdentifier? identifier = null;
			//Filter symbols if caret is in middle of identifier
			SyntaxNode? syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);
			if (syntaxRoot != null && syntaxRoot.FindToken(position).Parent is IdentifierNameSyntax ins)
			{
				var ident = ins.Identifier;
				int start = ident.Span.Start;
				identifier = new TextEditCompletionIdentifier(ident.Text, start, ident.Span.End);
				string prefix = ident.Text[..(position - start)];
				if (prefix.Length != 0)
					symbols = symbols.Where(x => x.Name.Contains(prefix, StringComparison.InvariantCultureIgnoreCase));
			}

			return new TextEditCompletionResult([.. symbols.GroupBy(static x => x.Name).Select(static x => new TextEditCompletionItem(x.Key, [.. x])).OrderBy(static x => x.Text)], identifier);
		}

		record TextEditCompletionResult(IReadOnlyCollection<ITextEditCompletionItem> Items, TextEditCompletionIdentifier? Identifier) : ITextEditCompletionResult;

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

				string? _description;
				bool _descriptionLoaded;
				public string? Description
				{
					get
					{
						if (!_descriptionLoaded)
						{
							_descriptionLoaded = true;
							string? xmlDocumentation = _symbol.GetDocumentationCommentXml();
							if (!string.IsNullOrEmpty(xmlDocumentation))
							{
								try
								{
									XElement xmlDoc = XElement.Parse(xmlDocumentation);
									XElement? summaryElement = xmlDoc.Element("summary");
									if (summaryElement != null)
										_description = DocumentationHelper.CleanXmlText(summaryElement.Value);
								}
								catch
								{ }
							}
						}

						return _description;
					}
				}
			}
		}
	}
}

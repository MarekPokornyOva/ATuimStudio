using ATuimStudio.Extensions.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

namespace ATuimStudio.Extensions.TextEditReferences
{
	sealed class RoslynReferencesFinder : IReferencesFinder
	{
		readonly IDocumentService _documentService;
		public RoslynReferencesFinder(IDocumentService documentService)
		{
			_documentService = documentService;
		}

		#region FindAllReferences
		static readonly IAllReferencesResult _emptyAllReferencesResult = new AllReferencesResult([]);

		public async Task<IAllReferencesResult> FindAllReferencesAsync(string filename, int offset, CancellationToken cancellationToken)
		{
			(ISymbol? symbol, Document document) = await TryFindSymbolAsync(filename, offset, cancellationToken);
			if (symbol == default)
				return _emptyAllReferencesResult;

			// Find all references across the entire solution
			Solution solution = document.Project.Solution;
			IEnumerable<ReferencedSymbol> references = await SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken);

			List<ReferenceItem> result = new List<ReferenceItem>();
			foreach (ReferencedSymbol refSymbol in references)
			{
				// The symbol being referenced
				symbol = refSymbol.Definition;

				foreach (Location loc in symbol.Locations)
					if (TryCreateReferenceItem(loc, out ReferenceItem item))
						result.Add(item);

				// Locations where this symbol is referenced
				foreach (ReferenceLocation location in refSymbol.Locations)
					if (TryCreateReferenceItem(location.Location, out ReferenceItem item))
						result.Add(item);
			}
			return new AllReferencesResult(result);
		}

		sealed record AllReferencesResult(IReadOnlyCollection<ReferenceItem> References) : IAllReferencesResult;
		#endregion FindAllReferences

		#region FindDefinition
		static readonly IFindDefinitionResult _emptyFindDefinitionResult = new FindDefinitionResult(default);

		public async Task<IFindDefinitionResult> FindDefinitionAsync(string filename, int offset, CancellationToken cancellationToken)
		{
			(ISymbol? symbol, Document document) = await TryFindSymbolAsync(filename, offset, cancellationToken);
			if (symbol == default)
				return _emptyFindDefinitionResult;

			// Use SymbolFinder to get the actual definition (handles partial classes, etc.)
			ISymbol? definition = await SymbolFinder.FindSourceDefinitionAsync(symbol, document.Project.Solution, cancellationToken);

			foreach (Location loc in (definition ?? symbol).Locations)
				if (TryCreateReferenceItem(loc, out ReferenceItem item))
					return new FindDefinitionResult(item);

			return _emptyFindDefinitionResult;
		}

		sealed record FindDefinitionResult(ReferenceItem Definition) : IFindDefinitionResult;
		#endregion FindDefinition

		#region FindImplementations
		static readonly IFindImplementationsResult _emptyFindImplementationsResult = new FindImplementationsResult([]);

		public async Task<IFindImplementationsResult> FindImplementationsAsync(string filename, int offset, CancellationToken cancellationToken)
		{
			(ISymbol? symbol, Document document) = await TryFindSymbolAsync(filename, offset, cancellationToken);
			if (symbol == default)
				return _emptyFindImplementationsResult;
			Solution solution = document.Project.Solution;

			// Find all implementations of the symbol across the entire solution
			IEnumerable<ISymbol> implementations = symbol is INamedTypeSymbol named
				? (await SymbolFinder.FindImplementationsAsync(named, solution, true, null, cancellationToken))
					.Select<INamedTypeSymbol, ISymbol>(x => x)
				: await SymbolFinder.FindImplementationsAsync(symbol, solution, null, cancellationToken);

			List<ReferenceItem> result = new List<ReferenceItem>();
			foreach (ISymbol implSymbol in implementations)
				foreach (Location loc in implSymbol.Locations)
					if (TryCreateReferenceItem(loc, out ReferenceItem item))
						result.Add(item);
			
			return new FindImplementationsResult(result);
		}

		sealed record FindImplementationsResult(IReadOnlyCollection<ReferenceItem> Implementations) : IFindImplementationsResult;
		#endregion FindImplementations

		#region internal
		async Task<(ISymbol? symbol, Document document)> TryFindSymbolAsync(string filename, int offset, CancellationToken cancellationToken)
		{
			Document? document = _documentService.GetDocument(filename);
			if (document == null)
				return default;

			// Get the syntax tree and semantic model
			SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken);
			if (semanticModel == null)
				return default;
			//SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken);
			SyntaxNode root = semanticModel.SyntaxTree.GetRoot(cancellationToken);

			// Get the token at the specified offset
			SyntaxToken token = root.FindToken(offset);
			SyntaxNode? tokenParent = token.Parent;
			if (tokenParent == null)
				return default;

			// Get the symbol from the token
			ISymbol? symbol = semanticModel.GetSymbolInfo(tokenParent, cancellationToken).Symbol
				?? semanticModel.GetDeclaredSymbol(tokenParent, cancellationToken);
			if (symbol == null)
				return default;

			// Unwrap alias symbols to get the actual definition
			if (symbol.Kind == SymbolKind.Alias)
				symbol = ((IAliasSymbol)symbol).Target;

			return (symbol, document);
		}

		static bool TryCreateReferenceItem(Location loc, out ReferenceItem referenceItem)
		{
			string? filename;
			if (loc.IsInSource && (filename = loc.SourceTree?.FilePath) != null)
			{
				FileLinePositionSpan pos = loc.GetLineSpan();
				referenceItem = new ReferenceItem(filename, loc.SourceSpan.Start, pos.StartLinePosition.Line + 1, pos.StartLinePosition.Character + 1);
				return true;
			}
			referenceItem = default;
			return false;
		}
		#endregion internal
	}
}

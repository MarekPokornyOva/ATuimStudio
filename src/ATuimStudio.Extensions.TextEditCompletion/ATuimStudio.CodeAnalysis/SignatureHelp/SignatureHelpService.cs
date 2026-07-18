using ATuimStudio.Extensions.TextEditCompletion;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Xml.Linq;

namespace ATuimStudio.CodeAnalysis.SignatureHelp
{
	/// <summary>
	/// Service for retrieving method signature help and overload information.
	/// </summary>
	public static partial class SignatureHelpService
	{
		static readonly MethodOverloadResult _emptyResult = new MethodOverloadResult([], null);

		/// <summary>
		/// Gets all method overload symbols at the specified position with their documentation.
		/// </summary>
		/// <param name="document">The document to analyze.</param>
		/// <param name="position">The position in the document.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A list of method overloads with documentation comments and exceptions.</returns>
		public static async Task<MethodOverloadResult> GetMethodOverloadSymbolsAtPositionAsync(
			 Document document,
			 int position,
			 CancellationToken cancellationToken)
		{
			// Get the syntax tree and root
			//SyntaxTree? syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);
			//syntaxTree.GetRootAsync();
			//var root = await document.GetCompilationAsync(cancellationToken);
			SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken);
			if (semanticModel == null)
				return _emptyResult;

			// Find the token at the specified position
			SyntaxNode? syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);
			if (syntaxRoot == null)
				return _emptyResult;
			SyntaxNode? node = syntaxRoot.FindToken(position).Parent;
			if (node == null)
				return _emptyResult;

			// Find the invocation or ctor expression (method call)
			ExpressionSyntax? expressionSyntax = node.AncestorsAndSelf().Where(static x => x is InvocationExpressionSyntax || x is ObjectCreationExpressionSyntax).OfType<ExpressionSyntax>().FirstOrDefault();
			if (expressionSyntax == null)
				return _emptyResult;

			// Get symbol information for the invocation
			SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(expressionSyntax is InvocationExpressionSyntax ies ? ies.Expression : expressionSyntax, cancellationToken);
			if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
				return _emptyResult;

			// Get all overloads of this method
			List<IMethodSymbol> overloads = GetMethodOverloads(methodSymbol);
			if (overloads.Count == 0)
				return _emptyResult;

			const int bestCandidate = 0;
			return new MethodOverloadResult
				(
					// Extract documentation for each overload
					new MappingReadOnlyCollection<IMethodSymbol, MethodOverloadInfo>(overloads, ExtractMethodOverloadInfo),
					bestCandidate
				);
		}

		/// <summary>
		/// Gets all overloads of a method symbol.
		/// </summary>
		private static List<IMethodSymbol> GetMethodOverloads(IMethodSymbol methodSymbol)
		{
			List<IMethodSymbol> overloads = new List<IMethodSymbol>();

			// If the method is already from a type, get all members with the same name
			if (methodSymbol.ContainingType != null)
				overloads.AddRange(methodSymbol.ContainingType.GetMembers(methodSymbol.Name).OfType<IMethodSymbol>());
			// If it's a reduced extension method, get from original definition
			else if (methodSymbol.ReducedFrom != null)
				overloads.AddRange(methodSymbol.ReducedFrom.ContainingType.GetMembers(methodSymbol.ReducedFrom.Name).OfType<IMethodSymbol>());

			return overloads;
		}

		/// <summary>
		/// Extracts documentation information from a method symbol.
		/// </summary>
		private static MethodOverloadInfo ExtractMethodOverloadInfo(IMethodSymbol methodSymbol)
		{
			// Get XML documentation
			string? xmlDocumentation = methodSymbol.GetDocumentationCommentXml();
			string? summary = null;
			List<ParameterDocumentation>? parameters = null;
			string? returnDescription = null;
			List<ExceptionDocumentation>? exceptions = null;

			if (!string.IsNullOrEmpty(xmlDocumentation))
			{
				try
				{
					XElement xmlDoc = XElement.Parse(xmlDocumentation);

					// Extract summary
					XElement? summaryElement = xmlDoc.Element("summary");
					if (summaryElement != null)
						summary = DocumentationHelper.CleanXmlText(summaryElement.Value);

					// Extract parameter descriptions
					foreach (XElement paramElement in xmlDoc.Elements("param"))
					{
						string? paramName = paramElement.Attribute("name")?.Value;
						if (!string.IsNullOrEmpty(paramName))
							(parameters ??= []).Add(new ParameterDocumentation
							(
								paramName,
								DocumentationHelper.CleanXmlText(paramElement.Value)
							));
					}

					// Extract return description
					XElement? returnsElement = xmlDoc.Element("returns");
					if (returnsElement != null)
						returnDescription = DocumentationHelper.CleanXmlText(returnsElement.Value);

					// Extract exception information
					foreach (XElement exceptionElement in xmlDoc.Elements("exception"))
					{
						string? exceptionType = exceptionElement.Attribute("cref")?.Value;
						if (!string.IsNullOrEmpty(exceptionType))
						{
							// Clean up cref format (T: -> remove prefix)
							int pos = exceptionType.IndexOf(':');
							if (pos != -1)
								exceptionType = exceptionType[(pos + 1)..];

							(exceptions ??= []).Add(new ExceptionDocumentation
							(
								exceptionType,
								DocumentationHelper.CleanXmlText(exceptionElement.Value)
							));
						}
					}
				}
				catch
				{
					// If XML parsing fails, continue without documentation
				}
			}

			return new MethodOverloadInfo
			(
				methodSymbol,
				methodSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
				summary,
				parameters ?? [],
				returnDescription,
				exceptions ?? []
			);
		}
	}
}

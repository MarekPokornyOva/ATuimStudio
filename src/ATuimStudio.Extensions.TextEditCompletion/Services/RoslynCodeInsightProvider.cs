using ATuimStudio.CodeAnalysis.SignatureHelp;
using ATuimStudio.Extensions.Core;
using Microsoft.CodeAnalysis;

namespace ATuimStudio.Extensions.TextEditCompletion
{
	sealed class RoslynCodeInsightProvider : ICodeInsightProvider
	{
		readonly IDocumentService _documentService;
		public RoslynCodeInsightProvider(IDocumentService documentService)
		{
			_documentService = documentService;
		}

		static readonly CodeInsightResult _emptyResult = new CodeInsightResult([], null);
		public async Task<ICodeInsightResult> GetAsync(string path, int position, CancellationToken cancellationToken)
		{
			Document? document = _documentService.GetDocument(path);
			if (document == null)
				return _emptyResult;

			MethodOverloadResult result = await SignatureHelpService.GetMethodOverloadSymbolsAtPositionAsync(document, position, cancellationToken);
			return result.Overloads.Count == 0
				? _emptyResult
				: new CodeInsightResult(new MappingReadOnlyCollection<MethodOverloadInfo, ICodeInsightMethodOverload>(result.Overloads, MapOverloadItem), result.BestCandidate);
		}

		static CodeInsightMethodOverload MapOverloadItem(MethodOverloadInfo overload)
			=> new CodeInsightMethodOverload(
				overload.Signature,
				overload.Summary,
				new MappingReadOnlyCollection<ParameterDocumentation, ICodeInsightMethodParameter>(overload.Parameters, static x => new CodeInsightMethodParameter(x.Name, x.Description)),
				overload.ReturnDescription,
				new MappingReadOnlyCollection<ExceptionDocumentation, ICodeInsightMethodException>(overload.Exceptions, static x => new CodeInsightMethodException(x.ExceptionType, x.Description))
				);

		sealed record CodeInsightResult
		(
			IReadOnlyCollection<ICodeInsightMethodOverload> Overloads,
			int? BestCandidate
		) : ICodeInsightResult;

		sealed record CodeInsightMethodOverload
		(
			string Signature,
			string? Summary,
			IReadOnlyCollection<ICodeInsightMethodParameter> Parameters,
			string? ReturnDescription,
			IReadOnlyCollection<ICodeInsightMethodException> Exceptions
		) : ICodeInsightMethodOverload;

		sealed record CodeInsightMethodParameter
		(
			string Name,
			string Description
		) : ICodeInsightMethodParameter;

		sealed record CodeInsightMethodException
		(
			string ExceptionType,
			string Description
		) : ICodeInsightMethodException;
	}
}

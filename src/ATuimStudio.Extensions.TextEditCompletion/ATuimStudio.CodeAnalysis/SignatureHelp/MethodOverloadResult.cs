using Microsoft.CodeAnalysis;

namespace ATuimStudio.CodeAnalysis.SignatureHelp
{
	public readonly record struct MethodOverloadResult
	(
		IReadOnlyCollection<MethodOverloadInfo> Overloads,
		int? BestCandidate
	);

	/// <summary>
	/// Represents a single method overload with its documentation and exceptions.
	/// </summary>
	public readonly record struct MethodOverloadInfo
	(
		IMethodSymbol Symbol,
		string Signature,
		string? Summary,
		IReadOnlyCollection<ParameterDocumentation> Parameters,
		string? ReturnDescription,
		IReadOnlyCollection<ExceptionDocumentation> Exceptions
	);

	/// <summary>
	/// Represents parameter documentation.
	/// </summary>
	public readonly record struct ParameterDocumentation
	(
		string Name,
		string Description
	);

	/// <summary>
	/// Represents exception documentation.
	/// </summary>
	public readonly record struct ExceptionDocumentation
	(
		string ExceptionType,
		string Description
	);
}

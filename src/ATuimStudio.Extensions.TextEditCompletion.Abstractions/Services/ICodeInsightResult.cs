namespace ATuimStudio.Extensions.TextEditCompletion
{
	public interface ICodeInsightResult
	{
		IReadOnlyCollection<ICodeInsightMethodOverload> Overloads { get; }
		int? BestCandidate { get; }
	}

	public interface ICodeInsightMethodOverload
	{
		string Signature { get; }
		string? Summary { get; }
		IReadOnlyCollection<ICodeInsightMethodParameter> Parameters { get; }
		string? ReturnDescription { get; }
		IReadOnlyCollection<ICodeInsightMethodException> Exceptions { get; }
	}

	public interface ICodeInsightMethodParameter
	{
		string Name { get; }
		string Description { get; }
	}

	public interface ICodeInsightMethodException
	{
		string ExceptionType { get; }
		string Description { get; }
	}	
}

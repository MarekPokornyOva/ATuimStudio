namespace ATuimStudio.Extensions.TextEditCompletion
{
	public interface ITextEditCompletionResult
	{
		IReadOnlyCollection<ITextEditCompletionItem> Items { get; }
		TextEditCompletionIdentifier? Identifier { get; }
	}

	public interface ITextEditCompletionItem
	{
		string Text { get; }
		string Label { get; }
		double Priority { get; }
		IReadOnlyCollection<ICodeEditCompletionItem> CodeItems { get; }
	}

	public interface ICodeEditCompletionItem
	{
		string Text { get; }
		CodeEditCompletionItemType Type { get; }
		string? Description { get; }
	}

	public enum CodeEditCompletionItemType
	{
		Other,
		Method
	}

	public record struct TextEditCompletionIdentifier(string Text, int Start, int End);
}

namespace ATuimStudio.Extensions.TextEditCompletion
{
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
	}

	public enum CodeEditCompletionItemType
	{
		Other,
		Method
	}
}

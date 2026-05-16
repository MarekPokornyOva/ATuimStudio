namespace ATuimStudio
{
	public interface ICodeDiagnosticsManager
	{
		void Refresh(string path);
		IReadOnlyCollection<IDiagnosticsItem> DiagnosticsItems { get; }
		event EventHandler? Updated;
	}

	public interface IDiagnosticsItem
	{
		string Code { get; }
		string Description { get; }
		DiagnosticsItemSeverity Severity { get; }
		string Path { get; }
		int Line { get; }
		int Character { get; }
		string ProjectName { get; }
	}

	public enum DiagnosticsItemSeverity
	{
		Info,
		Warning,
		Error,
	}
}

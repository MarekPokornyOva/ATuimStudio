namespace ATuimStudio.Extensions.Build
{
	public interface IBuildResult
	{
		bool Success { get; }
		IReadOnlyCollection<IBuildResultItem> Items { get; }
		string? OutputPath { get; }
	}

	public interface IBuildResultItem
	{
		BuildResultItemSeverity Severity { get; }
		string Code { get; }
		string Message { get; }
		BuildResultItemLocation Location { get; }
	}

	public enum BuildResultItemSeverity
	{
		Info = 1,
		Warning = 2,
		Error = 3,
	}

	public record struct BuildResultItemLocation(string Path, int Line, int Character);
}

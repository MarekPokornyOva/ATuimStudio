using Avalonia;

namespace ATuimStudio
{
	sealed class TopLevelVisualProvider : ITopLevelVisualProvider
	{
		public Visual Visual { get; internal set; } = default!;
	}
}

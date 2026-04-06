using ATuimStudio.Extensions.Core;

namespace ATuimStudio.Models
{
	public interface IFileOpenEvent
	{
		IProjectFileData FileData { get; }
	}

	public record FileOpenEvent(IProjectFileData FileData) : IFileOpenEvent;
}

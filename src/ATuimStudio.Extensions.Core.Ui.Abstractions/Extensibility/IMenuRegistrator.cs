using Avalonia.Input;

namespace ATuimStudio.Extensibility
{
	public interface IMenuRegistrator
	{
		void Register(IEnumerable<(string Title, int Priority)> segments, string commandCode, KeyGesture? gesture);
	}
}

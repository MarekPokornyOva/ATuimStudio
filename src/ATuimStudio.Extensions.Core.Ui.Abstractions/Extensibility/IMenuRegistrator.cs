using Avalonia.Input;

namespace ATuimStudio.Extensibility
{
	public interface IMenuRegistrator
	{
		void Register(IEnumerable<string> segments, string commandCode, KeyGesture? gesture);
	}
}

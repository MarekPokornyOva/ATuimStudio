using System.Windows.Input;

namespace ATuimStudio.Extensibility
{
	public interface ICommandRegistrator
	{
		IServiceProvider ServiceProvider { get; }
		void Register(string commandCode, ICommand command, Func<Stream>? imageDataProvider);
	}
}

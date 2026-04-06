using AvaloniaEdit;

namespace ATuimStudio.Extensibility
{
	public interface IEditorDecoratorRegistrator
	{
		void Register(Action<IEditorDecoratorRegistratorContext> callback);
	}

	public interface IEditorDecoratorRegistratorContext
	{
		TextEditor Editor { get; }
		IServiceProvider ServiceProvider { get; }
	}
}

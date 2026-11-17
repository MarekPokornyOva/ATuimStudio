using AvaloniaEdit;

namespace ATuimStudio.Extensibility
{
	public interface IEditorDecoratorRegistrator
	{
		void Register(string?[] languageIds, Action<IEditorDecoratorRegistratorContext> callback);
	}

	public interface IEditorDecoratorRegistratorContext
	{
		TextEditor Editor { get; }
		IServiceProvider ServiceProvider { get; }
	}
}

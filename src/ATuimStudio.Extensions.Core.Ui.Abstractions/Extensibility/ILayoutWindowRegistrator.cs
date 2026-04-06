using Dock.Model.Core;

namespace ATuimStudio.Extensibility
{
	public interface ILayoutWindowRegistrator
	{
		void Register(string parentIds, Func<ILayoutWindowRegistratorContext, IDockable> factory);
	}

	public interface ILayoutWindowRegistratorContext
	{
		T CreateViewModel<T>(string id, string title) where T : IDockable;
	}
}

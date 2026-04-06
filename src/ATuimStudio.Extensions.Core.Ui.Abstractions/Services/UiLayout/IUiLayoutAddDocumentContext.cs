using Dock.Model.Mvvm.Controls;

namespace ATuimStudio.Extensions.Core.Ui
{
	public interface IUiLayoutAddDocumentContext
	{
		T CreateViewModel<T>(string id, string title) where T : Document;
		Document? FindById(string id);
	}
}

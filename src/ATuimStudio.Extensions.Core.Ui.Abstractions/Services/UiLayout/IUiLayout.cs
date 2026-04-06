using Dock.Model.Mvvm.Controls;

namespace ATuimStudio.Extensions.Core.Ui
{
	public interface IUiLayout
	{
		void AddDocument(string id, Func<IUiLayoutAddDocumentContext, Document> factory);
	}
}

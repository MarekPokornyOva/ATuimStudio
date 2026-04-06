using ATuimStudio.Models;
using ATuimStudio.Extensions.Core;
using Dock.Model.Mvvm.Controls;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio.ViewModels
{
	public sealed class CustomDocumentDock : DocumentDock, IDisposable
	{
		[EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable CS8618
		public CustomDocumentDock()
		{ }
#pragma warning restore CS8618

		readonly IServiceProvider _serviceProvider;
		readonly ISub<IFileOpenEvent> _subFileOpenEvent;
		public CustomDocumentDock(IServiceProvider serviceProvider, ISub<IFileOpenEvent> subFileOpenEvent) : this()
		{
			_serviceProvider = serviceProvider;
			_subFileOpenEvent = subFileOpenEvent;
			subFileOpenEvent.Register(FileOpen);
		}

		public void Dispose()
		{
			_subFileOpenEvent.Unregister(FileOpen);
		}

		void FileOpen(IFileOpenEvent e)
			=> CreateNewDocument(e.FileData);

		void CreateNewDocument(IProjectFileData fileData)
		{
			string id = "doc-" + fileData.Path;
			AddDocument(id, sp => new DocumentViewModel(_serviceProvider.GetRequiredService<ISolutionService>()) { Id = id, Title = fileData.Name, FileData = fileData });
		}

		internal void AddDocument(string id, Func<IServiceProvider, Document> factory)
		{
			if (Factory == null)
				return;

			if (Factory.FindDockable(this, x => x.Id == id) is not Document document)
				Factory.AddDockable(this, document = factory(_serviceProvider));
			Factory.SetActiveDockable(document);
			Factory.SetFocusedDockable(this, document);
		}
	}
}

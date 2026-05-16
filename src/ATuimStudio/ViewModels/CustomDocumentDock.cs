using ATuimStudio.Models;
using ATuimStudio.Extensions.Core;
using Dock.Model.Mvvm.Controls;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using ATuimStudio.Views;
using Avalonia.Controls;

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

		static readonly ObjectFactory<DocumentViewModel> _documentViewModelFactory = ActivatorUtilities.CreateFactory<DocumentViewModel>(Type.EmptyTypes);
		static readonly object?[] _emptyArgs = [];
		void CreateNewDocument(IProjectFileData fileData)
		{
			string id = "doc-" + fileData.Path;
			AddDocument(id, fileData.Name,
				sp => { DocumentViewModel dvm = _documentViewModelFactory(sp, _emptyArgs); dvm.FileData = fileData; return dvm; },
				static sp => ActivatorUtilities.CreateInstance<DocumentView>(sp));
		}

		internal void AddDocument(string id, string title, Func<IServiceProvider, object> viewModelFactory, Func<IServiceProvider, Control> viewFactory)
		{
			if (Factory == null)
				return;

			if (Factory.FindDockable(this, x => x.Id == id) is not GeneralDocument document)
				Factory.AddDockable(this, document = new GeneralDocument(viewModelFactory(_serviceProvider), viewFactory) { Id = id, Title = title });
			Factory.SetActiveDockable(document);
			Factory.SetFocusedDockable(this, document);
		}
	}
}

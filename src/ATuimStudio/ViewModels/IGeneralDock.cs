using Avalonia.Controls;
using Dock.Model.Mvvm.Controls;

namespace ATuimStudio.ViewModels
{
	public interface IGeneralDockBase
	{
		Func<IServiceProvider, Control> ViewFactory { get; }
	}

	public interface IGeneralToolDockBase : IGeneralDockBase
	{
		string CacheId { get; }
		bool ViewModelCreated { get; }
		object GetViewModel(IServiceProvider serviceProvider);
	}

	sealed class GeneralTool : Tool, IGeneralToolDockBase
	{
		readonly Func<IServiceProvider, object> _viewModelFactory;
		object _viewModel = default!;
		internal GeneralTool(string cacheId, Func<IServiceProvider, object> viewModelFactory, Func<IServiceProvider, Control> viewFactory)
		{
			CacheId = cacheId;
			_viewModelFactory = viewModelFactory;
			ViewFactory = viewFactory;
		}

		public string CacheId { get; }
		public Func<IServiceProvider, Control> ViewFactory { get; }

		public bool ViewModelCreated { get; private set; }
		public object GetViewModel(IServiceProvider serviceProvider)
		{
			if (!ViewModelCreated)
			{
				_viewModel = _viewModelFactory(serviceProvider);
				ViewModelCreated = true;
			}
			return _viewModel;
		}
	}

	public interface IGeneralDocumentDockBase : IGeneralDockBase
	{
		object ViewModel { get; }
		bool TryGetView(out Control? view);
	}

	sealed class GeneralDocument : Document, IGeneralDocumentDockBase
	{
		WeakReference<Control>? _view;
		internal GeneralDocument(object viewModel, Func<IServiceProvider, Control> viewFactory)
		{
			ViewModel = viewModel;
			ViewFactory = sp => { Control view = viewFactory(sp); _view = new WeakReference<Control>(view); return view; };
		}

		public object ViewModel { get; }
		public Func<IServiceProvider, Control> ViewFactory { get; }

		public bool TryGetView(out Control? view)
		{
			if (_view != null && _view.TryGetTarget(out view))
				return true;
			view = default;
			return false;
		}
	}
}

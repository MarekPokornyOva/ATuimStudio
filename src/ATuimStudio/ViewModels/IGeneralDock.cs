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

		readonly TaskCompletionSource<object> _viewModelCreated = new TaskCompletionSource<object>();
		public Task<object> ViewModelCreatedTask => _viewModelCreated.Task;
		public bool ViewModelCreated => _viewModelCreated.Task.IsCompletedSuccessfully;

		public object GetViewModel(IServiceProvider serviceProvider)
		{
			if (!ViewModelCreated)
			{
				_viewModel = _viewModelFactory(serviceProvider);
				_viewModelCreated.SetResult(_viewModel);
			}
			return _viewModel;
		}
	}

	public interface IGeneralDocumentDockBase : IGeneralDockBase
	{
		object ViewModel { get; }
		bool TryGetView(out Control? view);
		bool IsDirty { get; set; }
	}

	sealed class GeneralDocument : Document, IGeneralDocumentDockBase
	{
		WeakReference<Control>? _view;
		readonly string _title;
		internal GeneralDocument(object viewModel, Func<IServiceProvider, Control> viewFactory, string title)
		{
			ViewModel = viewModel;

			if (viewModel is DocumentViewModel dvm)
				dvm.PropertyChanged += (s, e) => { if (e.PropertyName.EqualsOrdinal(nameof(DocumentViewModel.IsDirty))) IsDirty = dvm.IsDirty; };

			ViewFactory = sp => { Control view = viewFactory(sp); _view = new WeakReference<Control>(view); return view; };
			_title = title;
			Title = title;
		}

		public bool IsDirty { get; set { field = value; Title = value ? _title + "*" : _title; this.OnPropertyChanged(Title); } }

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

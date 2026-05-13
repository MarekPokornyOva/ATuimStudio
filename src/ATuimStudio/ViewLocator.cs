using ATuimStudio.ViewModels;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Core;
using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio
{
	public sealed class ViewLocator : IDataTemplate
	{
		internal static ServiceProvider ServiceProvider { get; set; } = default!;

		readonly Dictionary<string, WeakReference<Control>> _viewCache = new Dictionary<string, WeakReference<Control>>();
		readonly Lock _viewCacheLock = new Lock();

		public Control? Build(object? data)
		{
			if (data == null)
				return null;

			if (data is IGeneralToolDockBase toolDock)
				lock (_viewCacheLock)
				{
					if (!_viewCache.TryGetValue(toolDock.CacheId, out WeakReference<Control>? wr) || !wr.TryGetTarget(out Control? view))
					{
						view = toolDock.ViewFactory(ServiceProvider);
						view.DataContext = toolDock.GetViewModel(ServiceProvider);
						_viewCache[toolDock.CacheId] = new WeakReference<Control>(view);
					}
					return view;
				}

			if (data is IGeneralDocumentDockBase docDock)
			{
				Control view = docDock.ViewFactory(ServiceProvider);
				view.DataContext = docDock.ViewModel;
				return view;
			}

			Type vmType = data.GetType();
			string name = vmType.FullName!.Replace("ViewModel", "View");
			Type? viewType = vmType.Assembly.GetType(name);

			if (viewType == null)
				return new TextBlock { Text = "Not Found: " + name };
			return (Control)ActivatorUtilities.CreateInstance(ServiceProvider, viewType)!;
		}

		public bool Match(object? data)
			=> data is ObservableObject || data is IDockable;
	}
}

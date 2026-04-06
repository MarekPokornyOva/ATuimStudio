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

		public Control? Build(object? data)
		{
			if (data == null)
				return null;

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

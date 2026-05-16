using ATuimStudio.ViewModels;
using ATuimStudio.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace ATuimStudio;

public partial class App : Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
		PluginManager.Register(Styles.Add);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		// Line below is needed to remove Avalonia data validation.
		// Without this line you will get duplicate validations from both Avalonia and CT
		BindingPlugins.DataValidators.RemoveAt(0);

		//https://docs.avaloniaui.net/docs/guides/implementation-guides/how-to-implement-dependency-injection
		TopLevelVisualProvider topLevelVisualProvider = new TopLevelVisualProvider();
		ServiceProvider sp = AppServicesBuilder.BuildServiceProvider(topLevelVisualProvider);
		ViewLocator.ServiceProvider = sp;

		PluginManager.Register(sp);

		MainViewModel vm = ActivatorUtilities.CreateInstance<MainViewModel>(sp);
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			topLevelVisualProvider.Visual = desktop.MainWindow = new MainWindow(sp)
			{
				DataContext = vm,
				WindowState = WindowState.Maximized
			};
		}
		else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
		{
			topLevelVisualProvider.Visual = singleViewPlatform.MainView = new MainView
			{
				DataContext = vm
			};
		}

		base.OnFrameworkInitializationCompleted();
	}
}

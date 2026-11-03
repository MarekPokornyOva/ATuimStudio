using ATuimStudio.Extensions.Core;
using ATuimStudio.ViewModels;
using ATuimStudio.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

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
		//https://docs.avaloniaui.net/docs/guides/implementation-guides/how-to-implement-dependency-injection
		TopLevelVisualProvider topLevelVisualProvider = new TopLevelVisualProvider();
		ApplicationLifetimeService applicationLifetimeService = new ApplicationLifetimeService();
		ServiceProvider sp = AppServicesBuilder.BuildServiceProvider(topLevelVisualProvider, applicationLifetimeService);
		ViewLocator.ServiceProvider = sp;
		applicationLifetimeService.RegisterDisposable((IAsyncDisposable)sp);

		if (ApplicationLifetime is IControlledApplicationLifetime controlled)
			controlled.Exit += (sender, args) => applicationLifetimeService.Dispose();

			#region UserOptions
		IUserOptionsRepository userOptionsRepository = sp.GetRequiredService<IUserOptionsRepository>();
		DefaultUserOptionsManager userOptionsManager = sp.GetRequiredService<DefaultUserOptionsManager>();
		userOptionsManager.Changed += (_, values) =>
			userOptionsRepository.SaveAsync(values, CancellationToken.None).AwaitSync(true);
		userOptionsManager.LoadAsync(userOptionsRepository, CancellationToken.None).AwaitSync(true);
		#endregion UserOptions

		PluginManager.Register(sp);

		MainViewModel vm = ActivatorUtilities.CreateInstance<MainViewModel>(sp);
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			topLevelVisualProvider.Visual = desktop.MainWindow = new MainWindow
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

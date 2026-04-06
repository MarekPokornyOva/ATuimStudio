using ATuimStudio.Services;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace ATuimStudio.Views;

public partial class MainView : UserControl
{
	public MainView()
	{
		InitializeComponent();

		DataContextChanged += MainView_DataContextChanged;
	}

	void MainView_DataContextChanged(object? sender, EventArgs e)
	{
		if (DataContext is ATuimStudio.ViewModels.MainViewModel mvm)
		{
			DataContextChanged -= MainView_DataContextChanged;
			ApplyExtensions(mvm._pluginPartsRegistrator);
		}
	}

	void ApplyExtensions(IPluginPartsRegistrator pluginPartsRegistrator)
	{
		IReadOnlyDictionary<string, PluginPartsRegistrator.CommandRegistration> commands = pluginPartsRegistrator.GetCommands();

		Dictionary<PluginPartsRegistrator.CommandRegistration, Bitmap?> iconCache = new Dictionary<PluginPartsRegistrator.CommandRegistration, Bitmap?>();
		Bitmap? GetIcon(PluginPartsRegistrator.CommandRegistration commandRegistration)
		{
			if (iconCache.TryGetValue(commandRegistration, out Bitmap? icon))
				return icon;
			Func<Stream>? imageDataProvider = commandRegistration.ImageDataProvider;
			if (imageDataProvider != null)
				using (Stream str = imageDataProvider())
					icon = new Bitmap(str);
			iconCache.Add(commandRegistration, icon);
			return icon;
		}

		foreach (PluginPartsRegistrator.MenuRegistration menuRegistration in pluginPartsRegistrator.GetMenus())
		{
			if (!commands.TryGetValue(menuRegistration.CommandCode, out PluginPartsRegistrator.CommandRegistration command))
				continue;
	
			ItemsControl parentItem = MainMenu;
			MenuItem? menuItem = null;
			foreach (string segment in menuRegistration.Segments)
			{
				menuItem = parentItem.Items.OfType<MenuItem>().FirstOrDefault(x => x.Header is string title && title.EqualsOrdinal(segment));
				if (menuItem == null)
					parentItem.Items.Add(menuItem = new MenuItem { Header = segment });
				parentItem = menuItem;
			}
			if (menuItem != null)
			{
				menuItem.Command = command.Command;
				menuItem.Icon = new Image { Width = 16, Height = 16, Source = GetIcon(command) };
				menuItem.InputGesture = menuRegistration.Gesture;
			}
		}
	
		foreach (KeyValuePair<string, PluginPartsRegistrator.CommandRegistration> commandRegistrationItem in commands)
		{
			PluginPartsRegistrator.CommandRegistration commandRegistration = commandRegistrationItem.Value;
			Func<Stream>? imageDataProvider = commandRegistration.ImageDataProvider;
			if (imageDataProvider != null)
				Toolbar.Children.Add(new Button
				{
					Command = commandRegistration.Command,
					Content = new Image { Width = 16, Height = 16, Source = GetIcon(commandRegistration) }
				});
		}

		pluginPartsRegistrator.Clear();
	}
}

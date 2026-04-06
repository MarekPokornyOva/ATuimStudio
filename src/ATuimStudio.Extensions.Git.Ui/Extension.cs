using ATuimStudio.Extensibility;
using ATuimStudio.Extensions.Core.Ui;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio.Extensions.Git
{
	public sealed class Extension : ATuimStudio.Extensibility.UiExtension
	{
		const string CommandCode = "GitRepositoryOpen";
		public override void RegisterCommand(ICommandRegistrator commandRegistrator)
		{
			IUiLayoutAccessor uiLayoutAccessor = commandRegistrator.ServiceProvider.GetRequiredService<IUiLayoutAccessor>();
			commandRegistrator.Register(CommandCode, new AsyncRelayCommand(async () =>
				{
					const string id = "GitRepository";
					uiLayoutAccessor.UiLayout.AddDocument(id, static context =>
					{
						GitRepositoryViewModel res = context.CreateViewModel<GitRepositoryViewModel>(id, "Git repository");
						if (context.FindById("GitChanges") is GitViewModel gvm)
						{
							string? selectedRepo = gvm.SelectedRepo?.Path;
							if (selectedRepo != null)
							{
								string? selectedBranch = gvm.SelectedBranch?.Name;
								if (selectedBranch != null)
									res.SelectBranch(selectedRepo, selectedBranch);
							}
						}
						return res;
					});
				}),
				null
			);
		}

		public override void RegisterMenu(IMenuRegistrator menuRegistrator)
		{
			menuRegistrator.Register(["View", "Git repository"], CommandCode, null);
		}

		public override void RegisterLayoutWindow(ILayoutWindowRegistrator layoutWindowRegistrator)
		{
			layoutWindowRegistrator.Register(UiLayoutId.Left, context => context.CreateViewModel<GitViewModel>("GitChanges", "Git Changes"));
		}

		public override void RegisterServices(IServiceCollection services)
			=> GitServiceCollectionExtensions.AddGitServices(services);
	}
}

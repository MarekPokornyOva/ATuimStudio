using ATuimStudio.Extensibility;
using ATuimStudio.Extensions.Core.Ui;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ATuimStudio.Extensions.Git
{
	public sealed class Extension : ATuimStudio.Extensibility.UiExtension
	{
		const string CommandCode = "GitRepositoryOpen";
		const string IdGitChanges = "GitChanges";
		const string IdGitRepository = "GitRepository";
		readonly static Guid TypeGitChangesType = new Guid(0x746adff2, 0xf66e, 0x41c5, 0xb5, 0xde, 0x8a, 0x7, 0xa3, 0x1d, 0xa8, 0x73);
		readonly static Guid TypeGitRepository = new Guid(0xb301b536, 0xbd20, 0x40a8, 0xbf, 0xf9, 0x14, 0xd7, 0x70, 0xfc, 0xa6, 0x48);
		public override void RegisterCommand(ICommandRegistrator commandRegistrator)
		{
			commandRegistrator.Register(CommandCode, new AsyncRelayCommand(async () =>
				{
					IUiDocumentService documentService = commandRegistrator.ServiceProvider.GetRequiredService<IUiDocumentService>();
					documentService.AddSpecialDocument(IdGitRepository, "Git Repository", static sp =>
					{
						GitRepositoryViewModel res = ActivatorUtilities.CreateInstance<GitRepositoryViewModel>(sp);

						ILayoutManager layoutManager = sp.GetRequiredService<ILayoutManager>();
						if (layoutManager.TryFindViewModel(IdGitChanges) is GitViewModel gvm)
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
					},
					static _ => new GitRepositoryView());
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
			layoutWindowRegistrator.RegisterPaneFactory(TypeGitChangesType,
				static sp => ActivatorUtilities.CreateInstance<GitViewModel>(sp),
				static sp => new GitView());
			layoutWindowRegistrator.RegisterPaneFactory(TypeGitRepository,
				static sp => ActivatorUtilities.CreateInstance<GitRepositoryViewModel>(sp),
				static sp => new GitRepositoryView());

			layoutWindowRegistrator.RegisterParts(WellKnownLayoutConstants.LayoutBasic, static ctx =>
				ctx.Layout.FindPanesContainer(WellKnownLayoutConstants.IdMainNavigation)
					.AddPane(IdGitChanges, "Git Changes", TypeGitChangesType)
			);
		}

		public override void RegisterServices(IServiceCollection services)
			=> GitServiceCollectionExtensions.AddGitServices(services);
	}
}

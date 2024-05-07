// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class OpenDirectoryInNewPaneAction : ObservableObject, IAction
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();
		private ISidebarContext SidebarContext { get; } = Ioc.Default.GetRequiredService<ISidebarContext>();

		private ExecutableContextType _executableContextType;

		public string Label
			=> "OpenInNewPane".GetLocalizedResource();

		public string Description
			=> "OpenDirectoryInNewPaneDescription".GetLocalizedResource();

		public bool IsExecutable
			=> GetIsExecutable();

		public OpenDirectoryInNewPaneAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			if (GetIsExecutable() is false)
				return;

			switch (_executableContextType)
			{
				case ExecutableContextType.ContentPageContext:
					{
						NavigationHelpers.OpenInSecondaryPane(
							ContentPageContext.ShellPage!,
							ContentPageContext.SelectedItems[0]);

						break;
					}
				case ExecutableContextType.HomePageContext:
					{
						if (await DriveHelpers.CheckEmptyDrive(HomePageContext.RightClickedItem?.Path ?? string.Empty))
							return;

						ContentPageContext.ShellPage!.PaneHolder?.OpenPathInNewPane(
							HomePageContext.RightClickedItem?.Path ?? string.Empty);

						break;
					}
				case ExecutableContextType.SidebarContext:
					{
						if (await DriveHelpers.CheckEmptyDrive(HomePageContext.RightClickedItem?.Path ?? string.Empty))
							return;

						ContentPageContext.ShellPage!.PaneHolder?.OpenPathInNewPane(
							SidebarContext.RightClickedItem?.Path ?? string.Empty);

						break;
					}
			}
		}

		private bool GetIsExecutable()
		{
			if (ContentPageContext.SelectedItem is not null &&
				ContentPageContext.SelectedItem.IsFolder &&
				UserSettingsService.GeneralSettingsService.ShowOpenInNewPane)
			{
				_executableContextType = ExecutableContextType.ContentPageContext;
				return true;
			}
			else if (HomePageContext.IsAnyItemRightClicked &&
				HomePageContext.RightClickedItem is not null)
			{
				_executableContextType = ExecutableContextType.HomePageContext;
				return true;
			}
			else if (SidebarContext.IsItemRightClicked &&
				SidebarContext.RightClickedItem is not null)
			{
				_executableContextType = ExecutableContextType.SidebarContext;
				return true;
			}

			return false;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}

// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class OpenDirectoryInNewPaneAction : ObservableObject, IAction
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();

		private ActionExecutableType ExecutableType { get; set; }

		public string Label
			=> "OpenInNewPane".GetLocalizedResource();

		public string Description
			=> "OpenDirectoryInNewPaneDescription".GetLocalizedResource();

		public bool IsExecutable
			=> GetIsExecutable();

		public OpenDirectoryInNewPaneAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			switch (ExecutableType)
			{
				case ActionExecutableType.DisplayPageContext:
					ContentPageContext.ShellPage!.PaneHolder?.OpenPathInNewPane(
						ContentPageContext.ShellPage?.SlimContentPage.SelectedItem?.ItemPath ?? string.Empty);
					break;
				case ActionExecutableType.HomePageContext:
					ContentPageContext.ShellPage!.PaneHolder?.OpenPathInNewPane(
						HomePageContext.RightClickedItem?.Path ?? string.Empty);
					break;
			}

			return Task.CompletedTask;
		}

		private bool GetIsExecutable()
		{
			var executableInDisplayPage =
				ContentPageContext.SelectedItem is not null &&
				ContentPageContext.SelectedItem.IsFolder &&
				UserSettingsService.GeneralSettingsService.ShowOpenInNewPane;

			if (executableInDisplayPage)
				ExecutableType = ActionExecutableType.DisplayPageContext;

			var executableInHomePage =
				HomePageContext.IsAnyItemRightClicked;

			if (executableInHomePage)
				ExecutableType = ActionExecutableType.HomePageContext;

			return executableInDisplayPage || executableInHomePage;
		}

		private void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
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

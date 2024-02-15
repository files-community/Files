// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.System;

namespace Files.App.Actions
{
	internal class OpenInNewWindowItemAction : ObservableObject, IAction
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();

		private ActionExecutableType ExecutableType { get; set; }

		public string Label
			=> "OpenInNewWindow".GetLocalizedResource();

		public string Description
			=> "OpenInNewWindowDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.Enter, KeyModifiers.MenuCtrl);

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconOpenInNewWindow");

		public bool IsExecutable
			=> GetIsExecutable();

		public OpenInNewWindowItemAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			switch (ExecutableType)
			{
				case ActionExecutableType.DisplayPageContext:
					{
						foreach (ListedItem listedItem in ContentPageContext.ShellPage?.SlimContentPage.SelectedItems!)
						{
							var selectedItemPath = (listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath;
							var folderUri = new Uri($"files-uwp:?folder={@selectedItemPath}");
							await Launcher.LaunchUriAsync(folderUri);
						}
						break;
					}
				case ActionExecutableType.HomePageContext:
					{
						var selectedItemPath = HomePageContext.RightClickedItem?.Path ?? string.Empty;
						var folderUri = new Uri($"files-uwp:?folder={@selectedItemPath}");
						await Launcher.LaunchUriAsync(folderUri);
						break;
					}
			}
		}

		private bool GetIsExecutable()
		{
			var executableInDisplayPage =
				ContentPageContext.ShellPage is not null &&
				ContentPageContext.ShellPage.SlimContentPage is not null &&
				ContentPageContext.HasSelection &&
				ContentPageContext.SelectedItems.Count <= 5 &&
				ContentPageContext.SelectedItems.Where(x => x.IsFolder == true).Count() == ContentPageContext.SelectedItems.Count &&
				UserSettingsService.GeneralSettingsService.ShowOpenInNewWindow;

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

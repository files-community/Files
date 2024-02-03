// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.System;

namespace Files.App.Actions
{
	internal class OpenInNewWindowItemAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public string Label
			=> "OpenInNewWindow".GetLocalizedResource();

		public string Description
			=> "OpenInNewWindowDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.Enter, KeyModifiers.MenuCtrl);

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconOpenInNewWindow");

		public bool IsExecutable =>
			ContentPageContext.ShellPage is not null &&
			ContentPageContext.ShellPage.SlimContentPage is not null &&
			ContentPageContext.SelectedItems.Count <= 5 &&
			ContentPageContext.SelectedItems.Where(x => x.IsFolder == true).Count() == ContentPageContext.SelectedItems.Count &&
			UserSettingsService.GeneralSettingsService.ShowOpenInNewWindow;

		public OpenInNewWindowItemAction()
		{
			ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();
			UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			if (ContentPageContext.ShellPage?.SlimContentPage?.SelectedItems is null)
				return;

			List<ListedItem> items = ContentPageContext.ShellPage.SlimContentPage.SelectedItems;

			foreach (ListedItem listedItem in items)
			{
				var selectedItemPath = (listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath;
				var folderUri = new Uri($"files-uwp:?folder={@selectedItemPath}");

				await Launcher.LaunchUriAsync(folderUri);
			}
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

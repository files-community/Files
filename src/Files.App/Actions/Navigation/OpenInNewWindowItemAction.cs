// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.System;

namespace Files.App.Actions
{
	internal class OpenInNewWindowItemAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		private readonly IUserSettingsService userSettingsService;

		public string Label
			=> "OpenInNewWindow".GetLocalizedResource();

		public string Description
			=> "OpenInNewWindowDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.Enter, KeyModifiers.MenuCtrl);

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconOpenInNewWindow");

		public bool IsExecutable =>
			context.ShellPage is not null &&
			context.ShellPage.SlimContentPage is not null &&
			context.SelectedItems.Count <= 5 &&
			context.SelectedItems.Where(x => x.IsFolder == true).Count() == context.SelectedItems.Count &&
			userSettingsService.GeneralSettingsService.ShowOpenInNewWindow;

		public OpenInNewWindowItemAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			if (context.ShellPage?.SlimContentPage?.SelectedItems is null)
				return;

			List<ListedItem> items = context.ShellPage.SlimContentPage.SelectedItems;

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

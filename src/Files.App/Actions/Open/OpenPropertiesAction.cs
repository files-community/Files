// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Storage;

namespace Files.App.Actions
{
	internal class OpenPropertiesAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();

		private ActionExecutableType ExecutableType { get; set; }

		public string Label
			=> "OpenProperties".GetLocalizedResource();

		public string Description
			=> "OpenPropertiesDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconProperties");

		public HotKey HotKey
			=> new(Keys.Enter, KeyModifiers.Menu);

		public bool IsExecutable
			=> GetIsExecutable();

		public OpenPropertiesAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			switch (ExecutableType)
			{
				case ActionExecutableType.DisplayPageContext:
					{
						var layoutPage = ContentPageContext.ShellPage?.SlimContentPage!;
						var isFromBaseContextFlyout = false;

						EventHandler<object> flyoutClosed = null!;

						if (layoutPage?.BaseContextMenuFlyout.IsOpen ?? false)
						{
							isFromBaseContextFlyout = true;
							layoutPage.BaseContextMenuFlyout.Closed += flyoutClosed;
						}
						else if (layoutPage?.ItemContextMenuFlyout.IsOpen ?? false)
						{
							layoutPage.ItemContextMenuFlyout.Closed += flyoutClosed;
						}
						else
						{
							FilePropertiesHelpers.OpenPropertiesWindow(ContentPageContext.ShellPage!);
						}

						flyoutClosed = (s, e) =>
						{
							if (isFromBaseContextFlyout)
								layoutPage.BaseContextMenuFlyout.Closed -= flyoutClosed;
							else
								layoutPage.ItemContextMenuFlyout.Closed -= flyoutClosed;

							FilePropertiesHelpers.OpenPropertiesWindow(ContentPageContext.ShellPage!);
						};

						break;
					}
				case ActionExecutableType.HomePageContext:
					{
						EventHandler<object> flyoutClosed = null!;
						HomePageContext.ItemContextFlyoutMenu!.Closed += flyoutClosed;

						flyoutClosed = async (s, e) =>
						{
							HomePageContext.ItemContextFlyoutMenu!.Closed -= flyoutClosed;

							if (HomePageContext.RightClickedItem is WidgetDriveCardItem driveCardItem)
							{
								FilePropertiesHelpers.OpenPropertiesWindow(driveCardItem.Item, ContentPageContext.ShellPage!);
							}
							else if (HomePageContext.RightClickedItem is WidgetFileTagCardItem fileTagCardItem)
							{
								ListedItem listedItem = new(null!)
								{
									ItemPath = fileTagCardItem?.Path ?? string.Empty,
									ItemNameRaw = fileTagCardItem?.Name ?? string.Empty,
									PrimaryItemAttribute = StorageItemTypes.Folder,
									ItemType = "Folder".GetLocalizedResource(),
								};
								FilePropertiesHelpers.OpenPropertiesWindow(listedItem, ContentPageContext.ShellPage!);
							}
							else if (HomePageContext.RightClickedItem is WidgetFolderCardItem folderCardItem)
							{
								FilePropertiesHelpers.OpenPropertiesWindow(folderCardItem.Item!, ContentPageContext.ShellPage!);

							}
							else if (HomePageContext.RightClickedItem is WidgetRecentItem recentCardItem)
							{
								BaseStorageFile file = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(recentCardItem.Path));
								if (file is null)
									return;

								var listedItem = await UniversalStorageEnumerator.AddFileAsync(file, null, default);
								FilePropertiesHelpers.OpenPropertiesWindow(listedItem, ContentPageContext.ShellPage!);

							}
						};

						break;
					}
			}

			return Task.CompletedTask;
		}

		private bool GetIsExecutable()
		{
			var executableInDisplayPage =
				ContentPageContext.PageType is not ContentPageTypes.Home &&
				!(ContentPageContext.PageType is ContentPageTypes.SearchResults &&
				!ContentPageContext.HasSelection);

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
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.Folder):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}

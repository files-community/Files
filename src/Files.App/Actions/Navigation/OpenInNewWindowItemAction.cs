// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.System;

namespace Files.App.Actions
{
	internal sealed class OpenInNewWindowItemAction : ObservableObject, IAction
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();
		private ISidebarContext SidebarContext { get; } = Ioc.Default.GetRequiredService<ISidebarContext>();

		private ExecutableContextType _executableContextType;

		public string Label
			=> "OpenInNewWindow".GetLocalizedResource();

		public string Description
			=> "OpenInNewWindowDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.Enter, KeyModifiers.CtrlAlt);

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconOpenInNewWindow");

		public bool IsExecutable
			=> GetIsExecutable();

		public OpenInNewWindowItemAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			switch (_executableContextType)
			{
				case ExecutableContextType.ContentPageContext:
					{
						foreach (ListedItem listedItem in ContentPageContext.SelectedItems)
						{
							var selectedItemPath = (listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath;
							var folderUri = new Uri($"files-uwp:?folder={@selectedItemPath}");
							await Launcher.LaunchUriAsync(folderUri);
						}

						break;
					}
				case ExecutableContextType.HomePageContext:
					{
						var folderUri = new Uri($"files-uwp:?folder={HomePageContext.RightClickedItem?.Path ?? string.Empty}");
						await Launcher.LaunchUriAsync(folderUri);

						break;
					}
				case ExecutableContextType.SidebarContext:
					{
						var folderUri = new Uri($"files-uwp:?folder={SidebarContext.RightClickedItem?.Path ?? string.Empty}");
						await Launcher.LaunchUriAsync(folderUri);

						break;
					}
			}
		}

		private bool GetIsExecutable()
		{
			if (ContentPageContext.ShellPage is not null &&
				ContentPageContext.ShellPage.SlimContentPage is not null &&
				ContentPageContext.SelectedItems.Count is not 0 &&
				ContentPageContext.SelectedItems.Count <= 5 &&
				ContentPageContext.SelectedItems.Count(x => x.IsFolder) == ContentPageContext.SelectedItems.Count &&
				UserSettingsService.GeneralSettingsService.ShowOpenInNewWindow)
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

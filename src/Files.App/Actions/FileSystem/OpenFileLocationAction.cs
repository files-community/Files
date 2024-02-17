// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;

namespace Files.App.Actions
{
	internal class OpenFileLocationAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();

		private ActionExecutableType ExecutableType { get; set; }

		public string Label
			=> "OpenFileLocation".GetLocalizedResource();

		public string Description
			=> "OpenFileLocationDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(baseGlyph: "\uE8DA");

		public bool IsExecutable
			=> GetIsExecutable();

		public OpenFileLocationAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			switch (ExecutableType)
			{
				case ActionExecutableType.DisplayPageContext:
					{
						OpenShortcutLocation();
						break;
					}
				case ActionExecutableType.HomePageContext:
					{
						await OpenRecentItemLocation(HomePageContext.RightClickedItem?.Path ?? string.Empty);
						break;
					}
			}
		}

		private void OpenShortcutLocation()
		{
			if (ContentPageContext.ShellPage?.FilesystemViewModel is null)
				return;

			var item = ContentPageContext.SelectedItem as ShortcutItem;

			if (string.IsNullOrWhiteSpace(item?.TargetPath))
				return;

			// Check if destination path exists
			var folderPath = Path.GetDirectoryName(item.TargetPath);
			var destFolder = await ContentPageContext.ShellPage.FilesystemViewModel.GetFolderWithPathFromPathAsync(folderPath);

			if (destFolder)
			{
				ContentPageContext.ShellPage?.NavigateWithArguments(ContentPageContext.ShellPage.InstanceViewModel.FolderSettings.GetLayoutType(folderPath), new NavigationArguments()
				{
					NavPathParam = folderPath,
					SelectItems = new[] { Path.GetFileName(item.TargetPath.TrimPath()) },
					AssociatedTabInstance = ContentPageContext.ShellPage
				});
			}
			else if (destFolder == FileSystemStatusCode.NotFound)
			{
				await DialogDisplayHelper.ShowDialogAsync("FileNotFoundDialog/Title".GetLocalizedResource(), "FileNotFoundDialog/Text".GetLocalizedResource());
			}
			else
			{
				await DialogDisplayHelper.ShowDialogAsync("InvalidItemDialogTitle".GetLocalizedResource(),
					string.Format("InvalidItemDialogContent".GetLocalizedResource(), Environment.NewLine, destFolder.ErrorCode.ToString()));
			}
		}

		private async Task OpenRecentItemLocation(string path, bool isFolder = false)
		{
			try
			{
				if (!isFolder)
				{
					var directoryName = Path.GetDirectoryName(path);
					await Win32Helpers.InvokeWin32ComponentAsync(
						path,
						ContentPageContext.ShellPage!,
						workingDirectory: directoryName ?? string.Empty);
				}
				else
				{
					ContentPageContext.ShellPage!.NavigateWithArguments(
						ContentPageContext.ShellPage.InstanceViewModel.FolderSettings.GetLayoutType(path),
						new()
						{
							NavPathParam = path
						});
				}
			}
			catch (Exception) { }
		}

		private bool GetIsExecutable()
		{
			var executableInDisplayPage =
				ContentPageContext.ShellPage is not null &&
				ContentPageContext.HasSelection &&
				ContentPageContext.SelectedItem is ShortcutItem;

			if (executableInDisplayPage)
				ExecutableType = ActionExecutableType.DisplayPageContext;

			var executableInHomePage =
				HomePageContext.IsAnyItemRightClicked &&
				HomePageContext.RightClickedItem is WidgetFileTagCardItem or WidgetFolderCardItem;

			if (executableInHomePage)
				ExecutableType = ActionExecutableType.HomePageContext;

			return executableInDisplayPage || executableInHomePage;
		}

		private void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}

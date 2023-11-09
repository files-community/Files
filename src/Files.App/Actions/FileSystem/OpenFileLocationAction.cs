﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;

namespace Files.App.Actions
{
	internal class OpenFileLocationAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "OpenFileLocation".GetLocalizedResource();

		public string Description
			=> "OpenFileLocationDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(baseGlyph: "\uE8DA");

		public bool IsExecutable =>
			context.ShellPage is not null &&
			context.HasSelection &&
			context.SelectedItem is ShortcutItem;

		public OpenFileLocationAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			if (context.ShellPage?.FilesystemViewModel is null)
				return;

			var item = context.SelectedItem as ShortcutItem;

			if (string.IsNullOrWhiteSpace(item?.TargetPath))
				return;

			// Check if destination path exists
			var folderPath = Path.GetDirectoryName(item.TargetPath);
			var destFolder = await context.ShellPage.FilesystemViewModel.GetFolderWithPathFromPathAsync(folderPath);

			if (destFolder)
			{
				context.ShellPage?.NavigateWithArguments(context.ShellPage.InstanceViewModel.FolderSettings.GetLayoutType(folderPath), new NavigationArguments()
				{
					NavPathParam = folderPath,
					SelectItems = new[] { Path.GetFileName(item.TargetPath.TrimPath()) },
					AssociatedTabInstance = context.ShellPage
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

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}

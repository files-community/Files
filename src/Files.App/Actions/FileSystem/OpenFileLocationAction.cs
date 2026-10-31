// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class OpenFileLocationAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> Strings.OpenFileLocation.GetLocalizedResource();

		public string Description
			=> Strings.OpenFileLocationDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Open;

		public RichGlyph Glyph
			=> new(baseGlyph: "\uE8DA");

		public bool IsExecutable =>
			context.ShellPage is not null &&
			context.HasSelection &&
			context.SelectedItem is IShortcutItem;

		public OpenFileLocationAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage?.ShellViewModel is not { } shellViewModel)
				return;

			var item = context.SelectedItem as IShortcutItem;

			if (string.IsNullOrWhiteSpace(item?.TargetPath))
				return;

			// Check if destination path exists
			var folderPath = Path.GetDirectoryName(item.TargetPath);
			FilesystemResult<StorageFolderWithPath> destFolder = folderPath is null
				? new(null, FileSystemStatusCode.Generic)
				: await shellViewModel.GetFolderWithPathFromPathAsync(folderPath);

			if (destFolder)
			{
				if (context.ShellPage is not { } shellPage)
					return;

				shellPage.NavigateWithArguments(shellPage.InstanceViewModel.FolderSettings.GetLayoutType(folderPath), new NavigationArguments()
				{
					NavPathParam = folderPath,
					SelectItems = (string[])[Path.GetFileName(item.TargetPath.TrimPath())!],
					AssociatedTabInstance = shellPage
				});
			}
			else if (destFolder == FileSystemStatusCode.NotFound)
			{
				await DialogDisplayHelper.ShowDialogAsync(Strings.FileNotFoundDialogTitle.GetLocalizedResource(), Strings.FileNotFoundDialogText.GetLocalizedResource());
			}
			else
			{
				await DialogDisplayHelper.ShowDialogAsync(Strings.InvalidItemDialogTitle.GetLocalizedResource(),
					string.Format(Strings.InvalidItemDialogContent.GetLocalizedResource(), Environment.NewLine, destFolder.ErrorCode.ToString()));
			}
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}

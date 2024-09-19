// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Storage;

namespace Files.App.ViewModels.Dialogs
{
	public sealed class BulkRenameDialogViewModel : ObservableObject
	{
		private IContentPageContext context { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		// Properties

		public bool IsNameValid =>
			FilesystemHelpers.IsValidForFilename(fileName) && !fileName.Contains(".");

		public bool ShowNameWarning =>
			!string.IsNullOrEmpty(fileName) && !IsNameValid;


		private string fileName = string.Empty;
		public string FileName
		{
			get => fileName;
			set
			{
				if (SetProperty(ref fileName, value))
				{
					OnPropertyChanged(nameof(IsNameValid));
					OnPropertyChanged(nameof(ShowNameWarning));
				}
			}
		}

		// Commands
		public IAsyncRelayCommand CommitRenameCommand { get; private set; }

		public BulkRenameDialogViewModel()
		{
			CommitRenameCommand = new AsyncRelayCommand(DoCommitRenameAsync);
		}

		private async Task DoCommitRenameAsync()
		{
			if (context.ShellPage is null)
				return;

			await Task.WhenAll(context.SelectedItems.Select(item =>
			{
				var itemType = item.PrimaryItemAttribute == StorageItemTypes.Folder ? FilesystemItemType.Directory : FilesystemItemType.File;
				return context.ShellPage.FilesystemHelpers.RenameAsync(
					StorageHelpers.FromPathAndType(item.ItemPath, itemType),
					fileName + item.FileExtension,
					NameCollisionOption.GenerateUniqueName,
					true,
					false
				);
			}));

		}
	}
}
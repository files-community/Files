// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Dialogs;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Metadata;

namespace Files.App.Actions
{
	internal sealed class CompressIntoArchiveAction : BaseCompressArchiveAction
	{
		public override string Label
			=> "CreateArchive".GetLocalizedResource();

		public override string Description
			=> "CompressIntoArchiveDescription".GetLocalizedResource();

		public CompressIntoArchiveAction()
		{
		}

		public override async Task ExecuteAsync()
		{
			if (context.ShellPage is null)
				return;

			string[] sources = context.SelectedItems.Select(item => item.ItemPath).ToArray();
			string directory = string.Empty;
			string fileName = string.Empty;

			if (sources.Length is not 0)
			{
				// Get the current directory path
				directory = context.ShellPage.FilesystemViewModel.WorkingDirectory.Normalize();

				// Get the library save folder if the folder is library item
				if (App.LibraryManager.TryGetLibrary(directory, out var library) && !library.IsEmpty)
					directory = library.DefaultSaveFolder;

				// Gets the file name from the directory path
				fileName = SystemIO.Path.GetFileName(sources.Length is 1 ? sources[0] : directory);
			}

			var dialog = new CreateArchiveDialog
			{
				FileName = fileName,
			};

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				dialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			var result = await dialog.TryShowAsync();

			if (!dialog.CanCreate || result != ContentDialogResult.Primary)
				return;

			ICompressArchiveModel creator = new CompressArchiveModel(
				sources,
				directory,
				dialog.FileName,
				dialog.Password,
				dialog.FileFormat,
				dialog.CompressionLevel,
				dialog.SplittingSize);

			await CompressHelper.CompressArchiveAsync(creator);
		}
	}
}

// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class CompressIntoSevenZipAction : BaseCompressArchiveAction
	{
		public override string Label
			=> string.Format("CreateNamedArchive".GetLocalizedResource(), $"{StorageArchiveService.GenerateArchiveNameFromItems(context.SelectedItems)}.7z");

		public override string Description
			=> "CompressIntoSevenZipDescription".GetLocalizedResource();

		public CompressIntoSevenZipAction()
		{
		}

		public override Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage is null)
				return Task.CompletedTask;

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

			ICompressArchiveModel creator = new CompressArchiveModel(
				sources,
				directory,
				fileName,
				fileFormat: ArchiveFormats.SevenZip);

			return StorageArchiveService.CompressAsync(creator);
		}
	}
}

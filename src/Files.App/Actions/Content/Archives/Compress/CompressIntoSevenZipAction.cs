// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed partial class CompressIntoSevenZipAction : BaseCompressArchiveAction
	{
		public override string Label
			=> string.Format(Strings.CreateNamedArchive.GetLocalizedResource(), $"{StorageArchiveService.GenerateArchiveNameFromItems(context.SelectedItems)}.7z");

		public override string Description
			=> Strings.CompressIntoSevenZipDescription.GetLocalizedFormatResource(context.SelectedItems.Count);

		public CompressIntoSevenZipAction()
		{
		}

		public override Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage is null)
				return Task.CompletedTask;

			GetDestination(out var sources, out var directory, out var fileName);

			ICompressArchiveModel compressionModel = new CompressArchiveModel(
				sources,
				directory,
				fileName,
				Environment.ProcessorCount,
				fileFormat: ArchiveFormats.SevenZip);

			return StorageArchiveService.CompressAsync(compressionModel);
		}
	}
}

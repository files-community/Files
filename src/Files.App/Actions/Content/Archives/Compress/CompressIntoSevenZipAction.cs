// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class CompressIntoSevenZipAction : BaseCompressArchiveAction
	{
		public override string Label
			=> string.Format("CreateNamedArchive".GetLocalizedResource(), $"{CompressHelper.DetermineArchiveNameFromSelection(ContentPageContext.SelectedItems)}.7z");

		public override string Description
			=> "CompressIntoSevenZipDescription".GetLocalizedResource();

		public CompressIntoSevenZipAction()
		{
		}

		public override Task ExecuteAsync()
		{
			if (ContentPageContext.ShellPage is null)
				return Task.CompletedTask;

			var (sources, directory, fileName) = CompressHelper.GetCompressDestination(ContentPageContext.ShellPage);

			ICompressArchiveModel creator = new CompressArchiveModel(
				sources,
				directory,
				fileName,
				fileFormat: ArchiveFormats.SevenZip);

			return CompressHelper.CompressArchiveAsync(creator);
		}
	}
}

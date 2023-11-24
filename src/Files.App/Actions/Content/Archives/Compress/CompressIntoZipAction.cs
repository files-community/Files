// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class CompressIntoZipAction : BaseCompressArchiveAction
	{
		public override string Label
			=> string.Format("CreateNamedArchive".GetLocalizedResource(), $"{CompressHelper.DetermineArchiveNameFromSelection(context.SelectedItems)}.zip");

		public override string Description
			=> "CompressIntoZipDescription".GetLocalizedResource();

		public CompressIntoZipAction()
		{
		}

		public override Task ExecuteAsync()
		{
			if (context.ShellPage is null)
				return Task.CompletedTask;

			var (sources, directory, fileName) = CompressHelper.GetCompressDestination(context.ShellPage);

			ICompressArchiveModel creator = new CompressArchiveModel(
				sources,
				directory,
				fileName,
				fileFormat: ArchiveFormats.Zip);

			return CompressHelper.CompressArchiveAsync(creator);
		}
	}
}

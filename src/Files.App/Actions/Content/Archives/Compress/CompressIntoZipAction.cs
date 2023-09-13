// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	/// <summary>
	/// Represents action to compress into zip.
	/// </summary>
	internal sealed class CompressIntoZipAction : BaseCompressArchiveAction
	{
		public override string Label
			=> string.Format("CreateNamedArchive".GetLocalizedResource(), $"{ArchiveHelpers.DetermineArchiveNameFromSelection(context.SelectedItems)}.zip");

		public override string Description
			=> "CompressIntoZipDescription".GetLocalizedResource();

		public CompressIntoZipAction()
		{
		}

		public override Task ExecuteAsync()
		{
			var (sources, directory, fileName) = ArchiveHelpers.GetCompressDestination(context.ShellPage);

			IArchiveCreator creator = new ArchiveCreator
			{
				Sources = sources,
				Directory = directory,
				FileName = fileName,
				FileFormat = ArchiveFormats.Zip,
			};

			return ArchiveHelpers.CompressArchiveAsync(creator);
		}
	}
}

// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Dialogs;
using Microsoft.UI.Xaml.Controls;

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
			var (sources, directory, fileName) = ArchiveHelpers.GetCompressDestination(context.ShellPage);

			var dialog = new CreateArchiveDialog
			{
				FileName = fileName,
			};

			var result = await dialog.TryShowAsync();

			if (!dialog.CanCreate || result != ContentDialogResult.Primary)
				return;

			IArchiveCreator creator = new ArchiveCreator
			{
				Sources = sources,
				Directory = directory,
				FileName = dialog.FileName,
				Password = dialog.Password,
				FileFormat = dialog.FileFormat,
				CompressionLevel = dialog.CompressionLevel,
				SplittingSize = dialog.SplittingSize,
			};

			await ArchiveHelpers.CompressArchiveAsync(creator);
		}
	}
}

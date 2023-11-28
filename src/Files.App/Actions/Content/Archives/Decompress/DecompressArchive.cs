// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;
using System.IO;

namespace Files.App.Actions
{
	internal sealed class DecompressArchive : BaseDecompressArchiveAction
	{
		public override string Label
			=> "ExtractFiles".GetLocalizedResource();

		public override string Description
			=> "DecompressArchiveDescription".GetLocalizedResource();

		public override HotKey HotKey
			=> new(Keys.E, KeyModifiers.Ctrl);

		public DecompressArchive()
		{
		}

		public override Task ExecuteAsync()
		{
			if (context.ShellPage is null)
				return Task.CompletedTask;

			return DecompressHelper.DecompressArchiveAsync(context.ShellPage);
		}

		protected override bool CanDecompressInsideArchive()
		{
			return
				context.PageType == ContentPageTypes.ZipFolder &&
				!context.HasSelection &&
				context.Folder is not null &&
				FileExtensionHelpers.IsZipFile(Path.GetExtension(context.Folder.ItemPath));
		}
	}
}

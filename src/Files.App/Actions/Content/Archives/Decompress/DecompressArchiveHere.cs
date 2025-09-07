// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class DecompressArchiveHereAction : BaseDecompressArchiveAction
	{
		public override string Label
			=> Strings.ExtractHere.GetLocalizedResource();

		public override string Description
			=> Strings.DecompressArchiveHereDescription.GetLocalizedFormatResource(context.SelectedItems.Count);

		public DecompressArchiveHereAction()
		{
		}

		public override Task ExecuteAsync(object? parameter = null)
		{
			return DecompressArchiveHereAsync();
		}
	}
}

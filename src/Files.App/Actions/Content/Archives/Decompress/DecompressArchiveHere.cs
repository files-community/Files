// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed partial class DecompressArchiveHere : BaseDecompressArchiveAction
	{
		public override string Label
			=> Strings.ExtractHere.GetLocalizedResource();

		public override string Description
			=> Strings.DecompressArchiveHereDescription.GetLocalizedFormatResource(context.SelectedItems.Count);

		public DecompressArchiveHere()
		{
		}

		public override Task ExecuteAsync(object? parameter = null)
		{
			return DecompressArchiveHereAsync();
		}
	}
}

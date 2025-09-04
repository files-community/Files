// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class DecompressArchiveHereSmartAction : BaseDecompressArchiveAction
	{
		public override string Label
			=> Strings.ExtractHereSmart.GetLocalizedResource();

		public override string Description
			=> Strings.DecompressArchiveHereSmartDescription.GetLocalizedFormatResource(context.SelectedItems.Count);

		public override HotKey HotKey
			=> new(Keys.E, KeyModifiers.CtrlShift);

		public DecompressArchiveHereSmartAction()
		{
		}

		public override Task ExecuteAsync(object? parameter = null)
		{
			return DecompressArchiveHereAsync(true);
		}
	}
}

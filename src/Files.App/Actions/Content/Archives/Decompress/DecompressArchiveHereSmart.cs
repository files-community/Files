// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class DecompressArchiveHereSmart : BaseDecompressArchiveAction
	{
		public override string Label
			=> "ExtractHereSmart".GetLocalizedResource();

		public override string Description
			=> "DecompressArchiveHereSmartDescription".GetLocalizedResource();

		public override HotKey HotKey
			=> new(Keys.E, KeyModifiers.CtrlShift);

		public DecompressArchiveHereSmart()
		{
		}

		public override Task ExecuteAsync(object? parameter = null)
		{
			return DecompressHelper.DecompressArchiveHereAsync(context.ShellPage, true);
		}
	}
}

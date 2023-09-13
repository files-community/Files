// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	/// <summary>
	/// Represents action to decompress archive here.
	/// </summary>
	internal sealed class DecompressArchiveHere : BaseDecompressArchiveAction
	{
		public override string Label
			=> "ExtractHere".GetLocalizedResource();

		public override string Description
			=> "DecompressArchiveHereDescription".GetLocalizedResource();

		public DecompressArchiveHere()
		{
		}

		public override Task ExecuteAsync()
		{
			return ArchiveHelpers.DecompressArchiveHere(context.ShellPage);
		}
	}
}

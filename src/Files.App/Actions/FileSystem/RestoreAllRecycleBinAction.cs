// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;

namespace Files.App.Actions
{
	internal class RestoreAllRecycleBinAction : BaseUIAction, IAction
	{
		public string Label { get; } = "RestoreAllItems".GetLocalizedResource();

		public string Description => "RestoreAllRecycleBinDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconRestoreItem");

		public override bool IsExecutable =>
			UIHelpers.CanShowDialog &&
			RecycleBinHelpers.RecycleBinHasItems();

		public async Task ExecuteAsync()
		{
			await RecycleBinHelpers.RestoreRecycleBin();
		}
	}
}

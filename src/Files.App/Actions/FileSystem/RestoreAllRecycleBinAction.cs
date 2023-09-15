// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	/// <summary>
	/// Represents action to restore all storage items from Windows Recycle Bin.
	/// </summary>
	internal class RestoreAllRecycleBinAction : BaseUIAction, IAction
	{
		public string Label
			=> "RestoreAllItems".GetLocalizedResource();

		public string Description
			=> "RestoreAllRecycleBinDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconRestoreItem");

		public override bool IsExecutable =>
			UIHelpers.CanShowDialog &&
			RecycleBinHelpers.RecycleBinHasItems();

		public async Task ExecuteAsync()
		{
			await RecycleBinHelpers.RestoreRecycleBin();
		}
	}
}

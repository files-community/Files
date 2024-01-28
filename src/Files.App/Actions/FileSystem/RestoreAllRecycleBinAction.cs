// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class RestoreAllRecycleBinAction : BaseUIAction, IAction
	{
		private ITrashService RecycleBinService { get; } = Ioc.Default.GetRequiredService<ITrashService>();

		public string Label
			=> "RestoreAllItems".GetLocalizedResource();

		public string Description
			=> "RestoreAllRecycleBinDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconRestoreItem");

		public override bool IsExecutable =>
			UIHelpers.CanShowDialog &&
			RecycleBinService.HasItems();

		public async Task ExecuteAsync()
		{
			await RecycleBinService.RestoreTrashAsync();
		}
	}
}

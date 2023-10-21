// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class DeleteItemAction : BaseDeleteAction, IAction
	{
		public string Label
			=> "Delete".GetLocalizedResource();

		public string Description
			=> "DeleteItemDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new RichGlyph(opacityStyle: "ColorIconDelete");

		public HotKey HotKey
			=> new(Keys.Delete);

		public HotKey SecondHotKey
			=> new(Keys.D, KeyModifiers.Ctrl);

		public Task ExecuteAsync()
		{
			return DeleteItemsAsync(false);
		}
	}
}

// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Actions
{
	internal sealed partial class CopyItemAction : BaseTransferItemAction, IAction
	{
		public string Label
			=> Strings.Copy.GetLocalizedResource();

		public string Description
			=> Strings.CopyItemDescription.GetLocalizedFormatResource(ContentPageContext.SelectedItems.Count);

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Copy");

		public HotKey HotKey
			=> new(Keys.C, KeyModifiers.Ctrl);

		public CopyItemAction() : base()
		{
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			return ExecuteTransferAsync(DataPackageOperation.Copy);
		}
	}
}

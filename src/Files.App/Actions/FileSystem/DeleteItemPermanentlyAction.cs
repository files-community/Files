// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed partial class DeleteItemPermanentlyAction : BaseDeleteAction, IAction
	{
		public string Label
			=> Strings.DeletePermanently.GetLocalizedResource();

		public string Description
			=> Strings.DeleteItemPermanentlyDescription.GetLocalizedFormatResource(context.SelectedItems.Count);

		public HotKey HotKey
			=> new(Keys.Delete, KeyModifiers.Shift);

		public Task ExecuteAsync(object? parameter = null)
		{
			return DeleteItemsAsync(true);
		}
	}
}

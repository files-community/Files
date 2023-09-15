// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	/// <summary>
	/// Represents action to delete a storage item permanently.
	/// </summary>
	internal class DeleteItemPermanentlyAction : BaseDeleteAction, IAction
	{
		public string Label
			=> "DeletePermanently".GetLocalizedResource();

		public string Description
			=> "DeleteItemPermanentlyDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.Delete, KeyModifiers.Shift);

		public Task ExecuteAsync()
		{
			return DeleteItems(true);
		}
	}
}

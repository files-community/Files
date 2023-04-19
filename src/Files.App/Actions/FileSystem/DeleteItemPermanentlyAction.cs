﻿using Files.App.Commands;

namespace Files.App.Actions
{
	internal class DeleteItemPermanentlyAction : BaseDeleteAction, IAction
	{
		public string Label { get; } = "DeletePermanently".GetLocalizedResource();

		public string Description { get; } = "DeleteItemPermanentlyDescription".GetLocalizedResource();

		public HotKey HotKey { get; } = new(Keys.Delete, KeyModifiers.Shift);

		public Task ExecuteAsync()
		{
			return DeleteItems(true);
		}
	}
}

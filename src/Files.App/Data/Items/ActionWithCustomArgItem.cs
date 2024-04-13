// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Items
{
	public class ActionWithCustomArgItem
	{
		public CommandCodes CommandCode { get; set; }

		public string KeyBinding { get; set; } = string.Empty;

		public string Args { get; set; } = string.Empty;

		public ActionWithCustomArgItem(CommandCodes command, string keyBinding, string args = null)
		{
			CommandCode = command;
			KeyBinding = keyBinding;
			Args = args ?? string.Empty;
		}
	}
}

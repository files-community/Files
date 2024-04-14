// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Items
{
	public class ActionWithParameterItem
	{
		public CommandCodes CommandCode { get; set; }

		public string KeyBinding { get; set; } = string.Empty;

		public string CommandParameter { get; set; } = string.Empty;

		public ActionWithParameterItem(CommandCodes command, string keyBinding, string? parameter = null)
		{
			CommandCode = command;
			KeyBinding = keyBinding;
			CommandParameter = parameter ?? string.Empty;
		}
	}
}

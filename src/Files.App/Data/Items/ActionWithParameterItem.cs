// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Text.Json.Serialization;

namespace Files.App.Data.Items
{
	[Serializable]
	public class ActionWithParameterItem
	{
		[JsonPropertyName("CommandCode")]
		public string CommandCode { get; set; }

		[JsonPropertyName("CommandParameter")]
		public string CommandParameter { get; set; } = string.Empty;

		[JsonPropertyName("KeyBinding")]
		public string KeyBinding { get; set; } = string.Empty;

		[JsonConstructor]
		public ActionWithParameterItem(string command, string keyBinding, string? parameter = null)
		{
			CommandCode = command;
			KeyBinding = keyBinding;
			CommandParameter = parameter ?? string.Empty;
		}
	}
}

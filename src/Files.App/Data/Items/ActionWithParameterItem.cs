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
		public ActionWithParameterItem(string commandCode, string keyBinding, string? commandParameter = null)
		{
			CommandCode = commandCode;
			KeyBinding = keyBinding;
			CommandParameter = commandParameter ?? string.Empty;
		}
	}
}

// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Items
{
	[Serializable]
	public sealed class ToolbarItemSettingsEntry
	{
		[JsonPropertyName("CommandCode")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? CommandCode { get; set; }

		[JsonPropertyName("CommandGroup")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? CommandGroup { get; set; }

		[JsonPropertyName("ShowIcon")]
		public bool ShowIcon { get; set; } = true;

		[JsonPropertyName("ShowLabel")]
		public bool ShowLabel { get; set; } = false;

		[JsonConstructor]
		public ToolbarItemSettingsEntry(string? commandCode = null, string? commandGroup = null, bool showIcon = true, bool showLabel = false)
		{
			CommandCode = commandCode;
			CommandGroup = commandGroup;
			ShowIcon = showIcon;
			ShowLabel = showLabel;
		}
	}
}

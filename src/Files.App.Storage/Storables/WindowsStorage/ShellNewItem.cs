// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Storage
{
	/// <summary>
	/// Represents a ShellNew item with its type, file extension, description, and icon.
	/// </summary>
	public partial class ShellNewItem
	{
		public ContextMenuType Type { get; set; } = ContextMenuType.Normal;

		public uint Id { get; set; }

		public byte[]? Icon { get; set; }

		public string? Name { get; set; }
	}
}

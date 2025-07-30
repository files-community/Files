// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Storage
{
	/// <summary>
	/// Represents a Windows Shell ContextMenu item.
	/// </summary>
	public partial class WindowsContextMenuItem
	{
		public WindowsContextMenuType Type { get; set; }

		public uint Id { get; set; }

		public byte[]? Icon { get; set; }

		public string? Name { get; set; }
	}
}

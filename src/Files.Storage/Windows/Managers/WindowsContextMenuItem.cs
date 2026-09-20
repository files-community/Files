// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

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

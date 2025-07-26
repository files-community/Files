// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Storage
{
	/// <summary>
	/// Represents a Windows Shell ContextMenu item.
	/// </summary>
	public partial record WindowsContextMenuItem(uint Id = 0U, string? Name = null, byte[]? Icon = null, WindowsContextMenuType Type = WindowsContextMenuType.String, WindowsContextMenuState State = WindowsContextMenuState.Enabled);
}

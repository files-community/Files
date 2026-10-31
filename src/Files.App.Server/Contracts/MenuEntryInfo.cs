// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Server;

public sealed partial class MenuEntryInfo
{
	public int Id { get; }
	public int Submenu { get; }
	public uint Type { get; }
	public string Label { get; }
	public string Command { get; }
	public byte[] Icon { get; }

	internal MenuEntryInfo(Win32ContextMenuItem item, int submenu)
	{
		Id = item.ID;
		Submenu = submenu;
		Type = (uint)item.Type;
		Label = item.Label ?? string.Empty;
		Command = item.CommandString ?? string.Empty;
		Icon = item.Icon ?? [];
	}
}

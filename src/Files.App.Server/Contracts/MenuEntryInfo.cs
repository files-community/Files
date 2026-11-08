// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Server;

[Flags]
public enum MenuEntryFlags : uint
{
	None = 0,
	Disabled = 1,
	Checked = 2,
	Toggle = 4,
}

public sealed partial class MenuEntryInfo
{
	public int Id { get; }
	public int Submenu { get; }
	public uint Type { get; }
	public string Label { get; }
	public string Command { get; }
	public byte[] Icon { get; }
	public MenuEntryFlags Flags { get; }

	internal MenuEntryInfo(Win32ContextMenuItem item, int submenu)
	{
		Id = item.ID;
		Submenu = submenu;
		Type = (uint)item.Type;
		Label = item.Label ?? string.Empty;
		Command = item.CommandString ?? string.Empty;
		Icon = item.Icon ?? [];
		Flags = (item.IsEnabled ? MenuEntryFlags.None : MenuEntryFlags.Disabled)
			| (item.IsChecked ? MenuEntryFlags.Checked : MenuEntryFlags.None)
			| (item.IsToggle ? MenuEntryFlags.Toggle : MenuEntryFlags.None);
	}
}

// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Server;

public sealed partial class ShellNewEntryInfo
{
	public string Extension { get; }
	public string Name { get; }
	public string Command { get; }
	public string Template { get; }
	public byte[] IconData { get; }
	public byte[] Data { get; }
	public bool HasData { get; }

	internal ShellNewEntryInfo(ShellNewEntry entry)
	{
		Extension = entry.Extension ?? string.Empty;
		Name = entry.Name ?? string.Empty;
		Command = entry.Command ?? string.Empty;
		Template = entry.Template ?? string.Empty;
		IconData = entry.IconData ?? [];
		Data = entry.Data ?? [];
		HasData = entry.Data is not null;
	}
}

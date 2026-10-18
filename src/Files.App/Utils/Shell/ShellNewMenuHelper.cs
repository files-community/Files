// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Utils.Shell;

public static class ShellNewMenuHelper
{
	public static async Task<List<ShellNewEntry>> GetNewContextMenuEntries()
	{
		var entries = await Server.AppService.GetShellNewEntriesAsync();
		return entries.Select(ToEntry).ToList();
	}

	public static async Task<ShellNewEntry?> GetNewContextMenuEntryForType(string extension)
	{
		if (string.IsNullOrEmpty(extension))
			return null;
		var entry = await Server.AppService.GetShellNewEntryAsync(extension);
		return entry is null ? null : ToEntry(entry);
	}

	private static ShellNewEntry ToEntry(Server.ShellNewEntryInfo entry) => new()
	{
		Extension = entry.Extension,
		Name = entry.Name,
		Command = string.IsNullOrEmpty(entry.Command) ? null : entry.Command,
		Template = string.IsNullOrEmpty(entry.Template) ? null : entry.Template,
		IconData = entry.IconData,
		Data = entry.HasData ? entry.Data : null,
	};
}

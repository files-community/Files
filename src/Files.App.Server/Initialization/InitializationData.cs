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

public sealed partial class CloudProviderInfo
{
	public int Id { get; }
	public string Name { get; }
	public string SyncFolder { get; }
	public byte[] IconData { get; }

	internal CloudProviderInfo(ICloudProvider provider)
	{
		Id = (int)provider.ID;
		Name = provider.Name ?? string.Empty;
		SyncFolder = provider.SyncFolder ?? string.Empty;
		IconData = provider.IconData ?? [];
	}
}

public sealed partial class LibraryInfo
{
	public string FullPath { get; }
	public string AbsolutePath { get; }
	public string RelativePath { get; }
	public string DisplayName { get; }
	public bool IsPinned { get; }
	public string DefaultSaveFolder { get; }
	public string[] Folders { get; }

	internal LibraryInfo(ShellLibraryItem library)
	{
		FullPath = library.FullPath ?? string.Empty;
		AbsolutePath = library.AbsolutePath ?? string.Empty;
		RelativePath = library.RelativePath ?? string.Empty;
		DisplayName = library.DisplayName ?? string.Empty;
		IsPinned = library.IsPinned;
		DefaultSaveFolder = library.DefaultSaveFolder ?? string.Empty;
		Folders = library.Folders ?? [];
	}
}

public sealed partial class PinnedFolderInfo
{
	public string Path { get; }
	public string Name { get; }
	public byte[] Pidl { get; }
	public bool IsPinned { get; }

	internal PinnedFolderInfo(string path, string name, byte[] pidl, bool isPinned)
	{
		Path = path;
		Name = name;
		Pidl = pidl;
		IsPinned = isPinned;
	}
}

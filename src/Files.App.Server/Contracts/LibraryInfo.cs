// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Server;

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

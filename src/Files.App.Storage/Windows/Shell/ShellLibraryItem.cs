// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Items
{
	public sealed class ShellLibraryItem
	{
		public const string EXTENSION = ".library-ms";

		public static readonly string LibrariesPath = GetLibrariesPath();

		private static unsafe string GetLibrariesPath()
		{
			Windows.Win32.PInvoke.SHGetKnownFolderPath(Windows.Win32.PInvoke.FOLDERID_Libraries, 0, null, out var path).ThrowOnFailure();
			try
			{
				return path.ToString();
			}
			finally
			{
				Windows.Win32.PInvoke.CoTaskMemFree(path);
			}
		}

		/// <summary>
		/// Full path of library file.<br/>
		/// <br/>
		/// C:\Users\[username]\AppData\Roaming\Microsoft\Windows\Libraries\Documents.library-ms<br/>
		/// C:\Users\[username]\AppData\Roaming\Microsoft\Windows\Libraries\Custom library.library-ms
		/// </summary>
		public string? FullPath { get; set; }

		/// <summary>
		/// SIGDN.SIGDN_DESKTOPABSOLUTEPARSING<br/>
		/// <br/>
		/// ::{031E4825-7B94-4DC3-B131-E946B44C8DD5}\Documents.library-ms<br/>
		/// C:\Users\[username]\AppData\Roaming\Microsoft\Windows\Libraries\Custom library.library-ms
		/// </summary>
		public string? AbsolutePath { get; set; }

		/// <summary>
		/// SIGDN.SIGDN_PARENTRELATIVEPARSING<br/>
		/// <br/>
		/// {7B0DB17D-9CD2-4A93-9733-46CC89022E7C}<br/>
		/// Custom library.library-ms
		/// </summary>
		public string? RelativePath { get; set; }

		/// <summary>
		/// SIGDN.SIGDN_NORMALDISPLAY<br/>
		/// <br/>
		/// Documents (locale dependent based on desktop.ini file of the Libraries folder)<br/>
		/// Custom library (locale independent)
		/// </summary>
		public string? DisplayName { get; set; }

		public bool IsPinned { get; set; }

		public string? DefaultSaveFolder { get; set; }

		public string[]? Folders { get; set; }

		public ShellLibraryItem()
		{
		}
	}
}

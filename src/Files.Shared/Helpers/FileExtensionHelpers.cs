// Copyright (c) Files Community
// Licensed under the MIT License.

using ColorCode;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Files.Shared.Helpers
{
	/// <summary>
	/// Provides static extension for path extension.
	/// </summary>
	public static class FileExtensionHelpers
	{
		public static readonly FrozenDictionary<string, ILanguage> CodeFileExtensions = CodeFileExtensions_GetDictionary();

		private static readonly string[] CodeFileExtensionKeys = [.. CodeFileExtensions.Keys];

		private static readonly FrozenSet<string> _signableTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			".aab", ".apk", ".application", ".appx", ".appxbundle", ".arx", ".cab", ".cat", ".cbx",
			".cpl", ".crx", ".dbx", ".deploy", ".dll", ".doc", ".docm", ".dot", ".dotm", ".drx",
			".ear", ".efi", ".exe", ".jar", ".js", ".manifest", ".mpp", ".mpt", ".msi", ".msix",
			".msixbundle", ".msm", ".msp", ".nupkg", ".ocx", ".pot", ".potm", ".ppa", ".ppam", ".pps",
			".ppsm", ".ppt", ".pptm", ".ps1", ".psm1", ".psi", ".pub", ".sar", ".stl", ".sys", ".vbs",
			".vdw", ".vdx", ".vsd", ".vsdm", ".vss", ".vssm", ".vst", ".vstm", ".vsto", ".vsix", ".vsx", ".vtx",
			".vxd", ".war", ".wiz", ".wsf", ".xap", ".xla", ".xlam", ".xls", ".xlsb", ".xlsm", ".xlt",
			".xltm", ".xlsm", ".xsn"
		}.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

		private static FrozenDictionary<string, ILanguage> CodeFileExtensions_GetDictionary()
		{
			var items = new Dictionary<ILanguage, string>
			{
				[Languages.Aspx] = "aspx",
				[Languages.AspxCs] = "acsx",
				[Languages.Cpp] = "cpp,c++,cc,cp,cxx,h,h++,hh,hpp,hxx,inc,inl,ino,ipp,re,tcc,tpp",
				[Languages.CSharp] = "cs,cake,csx,linq",
				[Languages.Css] = "css,scss",
				[Languages.FSharp] = "fs,fsi,fsx",
				[Languages.Haskell] = "hs",
				[Languages.Html] = "razor,cshtml,vbhtml,svelte",
				[Languages.Java] = "java",
				[Languages.JavaScript] = "js,jsx",
				[Languages.Php] = "php",
				[Languages.PowerShell] = "pwsh,ps1,psd1,psm1",
				[Languages.Typescript] = "ts,tsx",
				[Languages.VbDotNet] = "vb,vbs",
				[Languages.Xml] = "xml,axml,xaml,xsd,xsl,xslt,xlf",
			};

			var dictionary = new Dictionary<string, ILanguage>();

			foreach (var item in items)
			{
				var extensions = item.Value.Split(',').Select(ext => $".{ext}");
				foreach (var extension in extensions)
				{
					dictionary.Add(extension, item.Key);
				}
			}

			return dictionary.ToFrozenDictionary();
		}

		/// <summary>
		/// Check if the file extension matches one of the specified extensions.
		/// </summary>
		/// <param name="filePathToCheck">Path or name or extension of the file to check.</param>
		/// <param name="extensions">List of the extensions to check.</param>
		/// <returns><c>true</c> if the filePathToCheck has one of the specified extensions; otherwise, <c>false</c>.</returns>
		public static bool HasExtension(string? filePathToCheck, params ReadOnlySpan<string> extensions)
		{
			if (string.IsNullOrWhiteSpace(filePathToCheck))
				return false;

			// Don't check folder paths to avoid issues
			// https://github.com/files-community/Files/issues/17094
			if (Directory.Exists(filePathToCheck))
				return false;

			string pathExtension = Path.GetExtension(filePathToCheck);
			foreach (string ext in extensions)
				if (pathExtension.Equals(ext, StringComparison.OrdinalIgnoreCase))
					return true;

			return false;
		}

		/// <summary>
		/// Checks if the file extension represents an image file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the fileExtensionToCheck is an image; otherwise, <c>false</c>.</returns>
		public static bool IsImageFile(string? fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".png", ".bmp", ".jpg", ".jpeg", ".jfif", ".gif", ".tiff", ".tif", ".webp", ".jxr");
		}

		/// <summary>
		/// Checks if the file can be set as wallpaper.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the fileExtensionToCheck is an image; otherwise, <c>false</c>.</returns>
		public static bool IsCompatibleToSetAsWindowsWallpaper(string? fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".png", ".bmp", ".jpg", ".jpeg", ".jfif", ".gif", ".tiff", ".tif", ".jxr", ".ico", ".webp");
		}

		/// <summary>
		/// Checks if the file extension represents an audio file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the fileExtensionToCheck is an audio file; otherwise, <c>false</c>.</returns>
		public static bool IsAudioFile(string? fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".mp3", ".m4a", ".wav", ".wma", ".aac", ".adt", ".adts", ".cda", ".flac");
		}

		/// <summary>
		/// Checks if the file extension represents a video file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the fileExtensionToCheck is a video file; otherwise, <c>false</c>.</returns>
		public static bool IsVideoFile(string? fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".mp4", ".webm", ".ogg", ".mov", ".qt", ".m4v", ".mp4v", ".3g2", ".3gp2", ".3gp", ".3gpp", ".mkv");
		}

		/// <summary>
		/// Checks if the file extension represents a PowerShell script.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the fileExtensionToCheck is a PowerShell script; otherwise, <c>false</c>.</returns>
		public static bool IsPowerShellFile(string fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".ps1");
		}

		/// <summary>
		/// Checks if the file extension represents a Batch file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the fileExtensionToCheck is a Batch file; otherwise, <c>false</c>.</returns>
		public static bool IsBatchFile(string fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".bat");
		}

		/// <summary>
		/// Checks if the file extension represents a zip file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the fileExtensionToCheck is a zip bundle file; otherwise, <c>false</c>.</returns>
		public static bool IsZipFile(string? fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".zip", ".msix", ".appx", ".msixbundle", ".appxbundle", ".7z", ".rar", ".tar", ".mcpack", ".mcworld", ".mrpack", ".jar", ".gz", ".lzh");
		}

		public static bool IsBrowsableZipFile(string? filePath, out string? ext)
		{
			if (string.IsNullOrWhiteSpace(filePath))
			{
				ext = null;

				return false;
			}

			// Only extensions we want to browse
			ext = new[] { ".zip", ".7z", ".rar", ".tar", ".gz", ".lzh", ".mrpack", ".jar" }
				.FirstOrDefault(x => filePath.Contains(x, StringComparison.OrdinalIgnoreCase));

			return ext is not null;
		}

		/// <summary>
		/// Checks if the file extension represents a driver inf file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the <c>filePathToCheck</c> is an inf file; otherwise <c>false</c>.</returns>
		public static bool IsInfFile(string? fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".inf");
		}

		/// <summary>
		/// Checks if the file extension represents a font file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the <c>filePathToCheck</c> is a font file; otherwise <c>false</c>.</returns>
		public static bool IsFontFile(string? fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".fon", ".otf", ".ttc", ".ttf");
		}

		/// <summary>
		/// Checks if the file extension represents a shortcut file.
		/// </summary>
		/// <param name="filePathToCheck">The file path to check.</param>
		/// <returns><c>true</c> if the <c>filePathToCheck</c> is a shortcut file; otherwise, <c>false</c>.</returns>
		public static bool IsShortcutFile(string? filePathToCheck)
		{
			return HasExtension(filePathToCheck, ".lnk");
		}

		/// <summary>
		/// Checks if the file extension represents a web link file.
		/// </summary>
		/// <param name="filePathToCheck">The file path to check.</param>
		/// <returns><c>true</c> if the <c>filePathToCheck</c> is a web link file; otherwise, <c>false</c>.</returns>
		public static bool IsWebLinkFile(string? filePathToCheck)
		{
			return HasExtension(filePathToCheck, ".url");
		}

		public static bool IsShortcutOrUrlFile(string? filePathToCheck)
		{
			return HasExtension(filePathToCheck, ".lnk", ".url");
		}

		/// <summary>
		/// Checks if the file extension represents an executable file.
		/// </summary>
		/// <param name="filePathToCheck">The file path to check.</param>
		/// <returns><c>true</c> if the <c>filePathToCheck</c> is an executable file; otherwise, <c>false</c>.</returns>
		public static bool IsExecutableFile(string? filePathToCheck, bool exeOnly = false)
		{
			return
				exeOnly
					? HasExtension(filePathToCheck, ".exe")
					: HasExtension(filePathToCheck, ".exe", ".bat", ".cmd", ".ahk");
		}

		/// <summary>
		/// Checks if the file extension represents an Auto Hot Key file.
		/// </summary>
		/// <param name="filePathToCheck">The file path to check.</param>
		/// <returns><c>true</c> if the <c>filePathToCheck</c> is an Auto Hot Key file; otherwise, <c>false</c>.</returns>
		public static bool IsAhkFile(string? filePathToCheck)
		{
			return HasExtension(filePathToCheck, ".ahk");
		}

		/// <summary>
		/// Checks if the file extension represents a CMD file.
		/// </summary>
		/// <param name="filePathToCheck">The file path to check.</param>
		/// <returns><c>true</c> if the <c>filePathToCheck</c> is a CMD file; otherwise, <c>false</c>.</returns>
		public static bool IsCmdFile(string? filePathToCheck)
		{
			return HasExtension(filePathToCheck, ".cmd");
		}

		/// <summary>
		/// Checks if the file extension represents an MSI installer file.
		/// </summary>
		/// <param name="filePathToCheck">The file path to check.</param>
		/// <returns><c>true</c> if the <c>filePathToCheck</c> is an MSI installer file; otherwise, <c>false</c>.</returns>
		public static bool IsMsiFile(string? filePathToCheck)
		{
			return HasExtension(filePathToCheck, ".msi");
		}

		/// <summary>
		/// Checks if the file extension represents a vhd disk file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the <c>filePathToCheck</c> is a vhd disk file; otherwise, <c>false</c>.</returns>
		public static bool IsVhdFile(string? fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".vhd", ".vhdx");
		}

		/// <summary>
		/// Checks if the file extension represents a screen saver file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the <c>filePathToCheck</c> is a screen saver file; otherwise, <c>false</c>.</returns>
		public static bool IsScreenSaverFile(string? fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".scr");
		}

		/// <summary>
		/// Checks if the file extension represents a media (audio/video) file.
		/// </summary>
		/// <param name="filePathToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the <c>filePathToCheck</c> is a media file; otherwise, <c>false</c>.</returns>
		public static bool IsMediaFile(string? filePathToCheck)
		{
			return HasExtension(
				filePathToCheck, ".mp4", ".m4v", ".mp4v", ".3g2", ".3gp2", ".3gp", ".3gpp",
				".mpg", ".mp2", ".mpeg", ".mpe", ".mpv", ".mkv", ".ogg", ".avi", ".wmv", ".mov", ".qt");
		}

		/// <summary>
		/// Checks if the file extension represents a certificate file.
		/// </summary>
		/// <param name="filePathToCheck"></param>
		/// <returns><c>true</c> if the <c>filePathToCheck</c> is a certificate file; otherwise, <c>false</c>.</returns>
		public static bool IsCertificateFile(string? filePathToCheck)
		{
			return HasExtension(filePathToCheck, ".cer", ".crt", ".der", ".pfx");
		}

		/// <summary>
		/// Checks if the file extension represents a script file.
		/// </summary>
		/// <param name="filePathToCheck"></param>
		/// <returns><c>true</c> if the <c>filePathToCheck</c> is a script file; otherwise, <c>false</c>.</returns>
		public static bool IsScriptFile(string? filePathToCheck)
		{
			return HasExtension(filePathToCheck, ".py", ".ahk", ".bat", ".cmd", ".ps1");
		}

		/// <summary>
		/// Checks if the file extension represents a system file.
		/// </summary>
		/// <param name="filePathToCheck"></param>
		/// <returns><c>true</c> if the <c>filePathToCheck</c> is a system file; otherwise, <c>false</c>.</returns>
		public static bool IsSystemFile(string? filePathToCheck)
		{
			return HasExtension(filePathToCheck, ".dll", ".exe", ".sys", ".inf");
		}

		/// <summary>
		/// Checks if the file extension matches a recognised code file extension.
		/// </summary>
		/// <param name="filePathToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the <c>filePathToCheck</c> is a code file; otherwise, <c>false</c>.</returns>
		public static bool IsCodeFile(string? filePathToCheck)
		{
			return HasExtension(filePathToCheck, CodeFileExtensionKeys);
		}

		/// <summary>
		/// Checks if the file extension represents an Adobe Acrobat PDF file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check</param>
		/// <returns><c>true</c> if the <c>filePathToCheck</c> is a PDF file; otherwise, <c>false</c>.</returns>
		public static bool IsPdfFile(string? fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".pdf");
		}

		/// <summary>
		/// Checks if the file extension represents an HTML file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check</param>
		/// <returns><c>true</c> if the <c>filePathToCheck</c> is an HTML file; otherwise, <c>false</c>.</returns>
		public static bool IsHtmlFile(string? fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".html", ".htm", ".xhtml", ".svg");
		}

		/// <summary>
		/// Checks if the file extension represents a markdown file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check</param>
		/// <returns><c>true</c> if the <c>filePathToCheck</c> is a markdown file; otherwise, <c>false</c>.</returns>
		public static bool IsMarkdownFile(string? fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".md", ".markdown");
		}

		/// <summary>
		/// Checks if the file extension represents a rich text file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check</param>
		/// <returns><c>true</c> if the <c>filePathToCheck</c> is a rich text file; otherwise, <c>false</c>.</returns>
		public static bool IsRichTextFile(string? fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".rtf");
		}

		/// <summary>
		/// Checks if the file extension represents a plain text file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check</param>
		/// <returns><c>true</c> if the <c>filePathToCheck</c> is a text file; otherwise, <c>false</c>.</returns>
		public static bool IsTextFile(string? fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".txt");
		}

		/// <summary>
		/// Check if the file is signable.
		/// </summary>
		/// <param name="filePathToCheck"></param>
		/// <returns><c>true</c> if the filePathToCheck is a signable file; otherwise, <c>false</c>.</returns>
		public static bool IsSignableFile(string? filePathToCheck, bool isExtension = false)
		{
			if (string.IsNullOrWhiteSpace(filePathToCheck))
				return false;

			if (!isExtension)
				filePathToCheck = Path.GetExtension(filePathToCheck);

			return _signableTypes.Contains(filePathToCheck);
		}
	}
}

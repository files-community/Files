// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Windows.System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using COMPRESSION_FORMAT = Windows.Win32.Storage.FileSystem.COMPRESSION_FORMAT;
using HRESULT = Windows.Win32.Foundation.HRESULT;
using HWND = Windows.Win32.Foundation.HWND;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides static helper for Win32.
	/// </summary>
	public static partial class Win32Helper
	{
		internal static string ToPowerShellStringLiteral(string? value)
			=> $"'{(value ?? string.Empty).Replace("'", "''", StringComparison.Ordinal)}'";

		public static async Task<string?> GetDefaultFileAssociationAsync(string filename, bool checkDesktopFirst = true)
		{
			// check if there exists an user choice first
			var userChoice = GetUserChoiceFileAssociation(filename);
			if (!string.IsNullOrEmpty(userChoice))
				return userChoice;

			return await GetFileAssociationAsync(filename, checkDesktopFirst);

		}

		public static string ExtractStringFromDLL(string file, int number)
		{
			using var lib = PInvoke.LoadLibrary(file);
			Span<char> result = stackalloc char[2048];

			int length = PInvoke.LoadString(lib, (uint)number, result, result.Length);

			return result[..length].ToString();
		}

		public static unsafe string?[] CommandLineToArgs(string commandLine)
		{
			if (string.IsNullOrEmpty(commandLine))
				return [];

			PWSTR* argv = PInvoke.CommandLineToArgv(commandLine, out int argc);
			if (argv is null)
				throw new Win32Exception();

			try
			{
				var args = new string?[argc];
				for (var i = 0; i < args.Length; i++)
				{
					args[i] = argv[i].ToString();
				}

				return args;
			}
			finally
			{
				PInvoke.LocalFree((HLOCAL)argv);
			}
		}

		public static byte[]? GetIconOverlay(string? path, int size, bool isDirectory) => ShellImage.GetIconOverlay(path, size, isDirectory);


		/// <summary>
		/// Returns an icon if returnIconOnly is true, otherwise a thumbnail will be returned if available.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="size"></param>
		/// <param name="isFolder"></param>
		/// <param name="iconOptions"></param>
		/// <returns></returns>
		public static byte[]? GetIcon(string? path, int size, bool isFolder, IconOptions iconOptions) => ShellImage.GetIcon(path, size, isFolder, iconOptions);

		public static async Task<bool> RunPowershellCommandAsync(string command, PowerShellExecutionOptions options, string? workingDirectory = null)
		{
			using Process process = CreatePowershellProcess(command, options, workingDirectory);
			using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(30 * 1000));

			try
			{
				process.Start();
				await process.WaitForExitAsync(cts.Token);
				return process.ExitCode == 0;
			}
			catch (OperationCanceledException)
			{
				return false;
			}
			catch (InvalidOperationException ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
				return false;
			}
			catch (Win32Exception)
			{
				// If user cancels UAC
				return false;
			}
		}

		public static bool RunPowershellCommand(string command, PowerShellExecutionOptions options, string? workingDirectory = null)
		{
			try
			{
				using Process process = CreatePowershellProcess(command, options, workingDirectory);

				process.Start();

				if (process.WaitForExit(30 * 1000))
					return process.ExitCode == 0;

				return false;
			}
			catch (Win32Exception)
			{
				// If user cancels UAC
				return false;
			}
		}

		private static readonly ConcurrentDictionary<(string File, int Index, int Size), IconFileInfo> _iconCache = new();

		public static IList<IconFileInfo> ExtractSelectedIconsFromDLL(string file, IList<int> indexes, int iconSize = 48)
			=> ShellImage.ExtractSelectedIconsFromDLL(file, indexes, iconSize);

		public static IList<IconFileInfo>? ExtractIconsFromDLL(string file)
		{
			var iconsList = new List<IconFileInfo>();

			using var icoCnt = PInvoke.ExtractIcon(file, uint.MaxValue);
			if (icoCnt.IsInvalid)
				return null;

			int count = icoCnt.DangerousGetHandle().ToInt32();
			icoCnt.SetHandleAsInvalid();
			if (count <= 0)
				return null;

			for (int i = 0; i < count; i++)
			{
				if (_iconCache.TryGetValue((file, i, -1), out var iconInfo))
				{
					iconsList.Add(iconInfo);
				}
				else
				{
					using var icon = PInvoke.ExtractIcon(file, (uint)i);
					using var image = GetBitmapFromHIcon(icon);
					if (image is null)
						continue;

					byte[] bitmapData = (byte[])(new ImageConverter().ConvertTo(image, typeof(byte[])) ?? Array.Empty<byte>());
					iconInfo = new IconFileInfo(bitmapData, i);
					_iconCache[(file, i, -1)] = iconInfo;
					iconsList.Add(iconInfo);
				}
			}

			return iconsList;
		}

		public static unsafe bool SetCustomDirectoryIcon(string? folderPath, string? iconFile, int iconIndex = 0)
		{
			if (folderPath is null)
				return false;

			fixed (char* iconPath = iconFile)
			{
				SHFOLDERCUSTOMSETTINGS settings = new()
				{
					dwSize = (uint)sizeof(SHFOLDERCUSTOMSETTINGS),
					dwMask = PInvoke.FCSM_ICONFILE,
					pszIconFile = iconPath,
					iIconIndex = iconIndex,
				};
				return PInvoke.SHGetSetFolderCustomSettings(ref settings, folderPath, PInvoke.FCS_FORCEWRITE).Succeeded;
			}
		}

		public static bool SetCustomFileIcon(string? filePath, string? iconFile, int iconIndex = 0)
		{
			if (filePath is null)
				return false;

			var success = FileOperationsHelpers.SetLinkIcon(filePath, iconFile, iconIndex);

			return success;
		}

		public static bool ConvertImageToIcoFile(string imagePath, string icoFilePath)
		{
			try
			{
				using var sourceImage = new Bitmap(imagePath);

				// ICO frames are capped at 256x256; don't upscale sources smaller than a standard frame size
				int largestFrameSize = Math.Min(Math.Max(sourceImage.Width, sourceImage.Height), 256);
				int[] frameSizes = [.. new[] { 16, 24, 32, 48, 256 }.Where(size => size < largestFrameSize), largestFrameSize];

				var pngFrames = new List<byte[]>(frameSizes.Length);
				foreach (int size in frameSizes)
				{
					using var frame = new Bitmap(size, size, PixelFormat.Format32bppArgb);
					using var graphics = Graphics.FromImage(frame);
					graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
					graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

					// Fit into the square frame preserving aspect ratio, centered on a transparent background
					double scale = Math.Min((double)size / sourceImage.Width, (double)size / sourceImage.Height);
					int scaledWidth = Math.Max(1, (int)Math.Round(sourceImage.Width * scale));
					int scaledHeight = Math.Max(1, (int)Math.Round(sourceImage.Height * scale));
					graphics.DrawImage(sourceImage, (size - scaledWidth) / 2, (size - scaledHeight) / 2, scaledWidth, scaledHeight);

					using var pngStream = new MemoryStream();
					frame.Save(pngStream, ImageFormat.Png);
					pngFrames.Add(pngStream.ToArray());
				}

				using var icoStream = new FileStream(icoFilePath, FileMode.Create, FileAccess.Write);
				using var writer = new BinaryWriter(icoStream);

				// ICONDIR header
				writer.Write((ushort)0); // Reserved
				writer.Write((ushort)1); // Type: icon
				writer.Write((ushort)frameSizes.Length);

				// ICONDIRENTRY table; a width/height byte of 0 means 256
				int frameDataOffset = 6 + 16 * frameSizes.Length;
				for (int i = 0; i < frameSizes.Length; i++)
				{
					byte sizeByte = (byte)(frameSizes[i] == 256 ? 0 : frameSizes[i]);
					writer.Write(sizeByte); // Width
					writer.Write(sizeByte); // Height
					writer.Write((byte)0); // Color palette size (none)
					writer.Write((byte)0); // Reserved
					writer.Write((ushort)1); // Color planes
					writer.Write((ushort)32); // Bits per pixel
					writer.Write((uint)pngFrames[i].Length);
					writer.Write((uint)frameDataOffset);
					frameDataOffset += pngFrames[i].Length;
				}

				// PNG-compressed frame data (supported in ICO since Windows Vista)
				foreach (var frameData in pngFrames)
					writer.Write(frameData);

				return true;
			}
			// GDI+ surfaces unsupported or corrupted image data as ArgumentException/OutOfMemoryException; IOException/UnauthorizedAccessException cover an unreadable source or unwritable destination
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Failed to convert '{ImagePath}' to an ICO file.", imagePath);
				return false;
			}
		}

		public static Task OpenFormatDriveDialog(string drive)
		{
			// Format requires elevation
			int driveIndex = drive.ToUpperInvariant()[0] - 'A';
			return RunPowershellCommandAsync($"-command \"$Signature = '[DllImport(\\\"shell32.dll\\\", SetLastError = false)]public static extern uint SHFormatDrive(IntPtr hwnd, uint drive, uint fmtID, uint options);'; $SHFormatDrive = Add-Type -MemberDefinition $Signature -Name \"Win32SHFormatDrive\" -Namespace Win32Functions -PassThru; $SHFormatDrive::SHFormatDrive(0, {driveIndex}, 0xFFFF, 0x0001)\"", PowerShellExecutionOptions.Elevated | PowerShellExecutionOptions.Hidden);
		}

		public static void SetVolumeLabel(string drivePath, string newLabel)
		{
			// Rename requires elevation
			RunPowershellCommand($"-command \"$Signature = '[DllImport(\\\"kernel32.dll\\\", SetLastError = false)]public static extern bool SetVolumeLabel(string lpRootPathName, string lpVolumeName);'; $SetVolumeLabel = Add-Type -MemberDefinition $Signature -Name \"Win32SetVolumeLabel\" -Namespace Win32Functions -PassThru; $SetVolumeLabel::SetVolumeLabel({ToPowerShellStringLiteral(drivePath)}, {ToPowerShellStringLiteral(newLabel)})\"", PowerShellExecutionOptions.Elevated | PowerShellExecutionOptions.Hidden);

			NotifyShellOfItemChange(drivePath);
		}

		public static void SetNetworkDriveLabel(string driveName, string newLabel)
		{
			RunPowershellCommand($"-command \"(New-Object -ComObject Shell.Application).NameSpace({ToPowerShellStringLiteral(driveName)}).Self.Name={ToPowerShellStringLiteral(newLabel)}\"", PowerShellExecutionOptions.Hidden);

			NotifyShellOfItemChange(driveName);
		}

		// Without this the shell keeps serving the cached display name
		private static unsafe void NotifyShellOfItemChange(string path)
		{
			// The shell parser doesn't accept extended length paths
			if (path.StartsWith(@"\\?\", StringComparison.Ordinal))
				path = path[4..];

			// A bare "U:" means the current directory on that drive rather than the volume root
			if (path.Length == 2 && path[1] == ':')
				path += '\\';

			fixed (char* pszPath = path)
				PInvoke.SHChangeNotify(SHCNE_ID.SHCNE_UPDATEITEM, SHCNF_FLAGS.SHCNF_PATHW | SHCNF_FLAGS.SHCNF_FLUSH, pszPath, null);
		}

		/// <summary>
		/// Returns the localized display name from the folder's desktop.ini, or null when there is none.
		/// </summary>
		public static unsafe string? GetLocalizedName(string path)
		{
			Span<char> resModule = stackalloc char[260];
			int resId;

			fixed (char* pszPath = path)
			fixed (char* pszResModule = resModule)
			{
				if (PInvoke.SHGetLocalizedName(pszPath, pszResModule, (uint)resModule.Length, &resId).Failed)
					return null;
			}

			int moduleLength = resModule.IndexOf('\0');
			if (moduleLength <= 0)
				return null;

			// A zero resource id means desktop.ini held a literal name rather than a resource reference
			if (resId == 0)
				return resModule[..moduleLength].ToString();

			// SHLoadIndirectString expands environment variables in the module path
			var source = $"@{resModule[..moduleLength]},-{resId}";
			Span<char> buffer = stackalloc char[512];
			fixed (char* pszSource = source)
			fixed (char* pszBuffer = buffer)
			{
				if (PInvoke.SHLoadIndirectString(pszSource, pszBuffer, (uint)buffer.Length, null).Failed)
					return null;
			}

			int length = buffer.IndexOf('\0');
			return length > 0 ? buffer[..length].ToString() : null;
		}

		public static Task<bool> MountVhdDisk(string vhdPath)
		{
			// Mounting requires elevation
			return RunPowershellCommandAsync($"-command \"Mount-DiskImage -ImagePath {ToPowerShellStringLiteral(vhdPath)}\"", PowerShellExecutionOptions.Elevated | PowerShellExecutionOptions.Hidden);
		}

		public static Bitmap? GetBitmapFromHBitmap(HBITMAP bitmap) => ShellImage.GetBitmapFromHBitmap(bitmap);

		private static Bitmap? GetBitmapFromHIcon(SafeHandle icon) => ShellImage.GetBitmapFromHIcon(icon);

		public static ITaskbarList4? CreateTaskbarObject()
		{
			try
			{
				var taskbar = TaskbarList.CreateInstance<ITaskbarList4>();
				taskbar.HrInit().ThrowOnFailure();
				return taskbar;
			}
			catch (Exception)
			{
				// explorer.exe is not running as a shell
				return null;
			}
		}

		public static IEnumerable<HWND> GetDesktopWindows()
		{
			HWND prevHwnd = HWND.Null;
			var windowsList = new List<HWND>();

			while (true)
			{
				prevHwnd = PInvoke.FindWindowEx(HWND.Null, prevHwnd, null, null);
				if (prevHwnd == HWND.Null)
					break;

				windowsList.Add(prevHwnd);
			}

			return windowsList;
		}

		public static void BringToForeground(IEnumerable<HWND> currentWindows)
		{
			CancellationTokenSource cts = new CancellationTokenSource();
			cts.CancelAfter(5 * 1000);

			Task.Run(async () =>
			{
				while (!cts.IsCancellationRequested)
				{
					await Task.Delay(500);

					var newWindows = GetDesktopWindows().Except(currentWindows).Where(x => PInvoke.IsWindowVisible(x) && !PInvoke.IsIconic(x));
					if (newWindows.Any())
					{
						foreach (var newWindow in newWindows)
						{
							PInvoke.SetWindowPos(
								newWindow,
								(HWND)(-1),
								0, 0, 0, 0,
								SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE);

							PInvoke.SetWindowPos(
								newWindow,
								(HWND)(-2),
								0, 0, 0, 0,
								SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE);
						}

						break;
					}
				}
			});
		}

		/// <summary>
		/// Gets file path from file FRN
		/// </summary>
		/// <param name="frn">File reference number</param>
		/// <param name="volumeHint">Drive containing the file (e.g. "C:\")</param>
		/// <returns>File path or null</returns>
		public static string? PathFromFileId(ulong frn, string volumeHint)
			=> FileIdentity.PathFromFileId(frn, volumeHint);

		public static unsafe void OpenFolderInExistingShellWindow(string folderPath)
		{
			if (TryOpenFolderInExistingShellWindow(folderPath))
				return;

			string operation = "open";
			string explorerPath = Environment.ExpandEnvironmentVariables("%windir%\\explorer.exe");
			fixed (char* operationPointer = operation, explorerPointer = explorerPath, folderPointer = folderPath)
				PInvoke.ShellExecute(HWND.Null, operationPointer, explorerPointer, folderPointer, null, SHOW_WINDOW_CMD.SW_SHOWNORMAL);
		}

		private static bool TryOpenFolderInExistingShellWindow(string folderPath)
		{
			try
			{
				using var target = SafetyExtensions.IgnoreExceptions(() => ShellFolderExtensions.GetShellItemFromPathOrPIDL(folderPath));
				if (target is null)
					return false;

				using var controlPanelCategoryView = new ShellItem("::{26EE0668-A00A-44D7-9371-BEB064C98683}");
				using var shellWindows = new ShellWindowsAutomation();
				int count = shellWindows.Count;
				for (int index = 0; index < count; index++)
				{
					using var window = shellWindows.GetWindow(index);
					if (window?.TryNavigate(target.PIDL, controlPanelCategoryView.PIDL) is true)
						return true;
				}
			}
			catch (Exception exception)
			{
				Debug.WriteLine(exception);
			}

			return false;
		}

		public static async Task<bool> InstallInf(string? filePath)
		{
			try
			{
				var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(30 * 1000));

				using Process process = new();
				process.StartInfo.FileName = "InfDefaultInstall.exe";
				process.StartInfo.Verb = "runas";
				process.StartInfo.UseShellExecute = true;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.Arguments = $"\"{filePath}\"";
				process.Start();

				await process.WaitForExitAsync(cts.Token);

				return true;
			}
			catch
			{
				return false;
			}
		}

		public static async Task InstallFontsAsync(string[] fontFilePaths, bool forAllUsers)
		{
			string fontDirectory = forAllUsers
				? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts")
				: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Fonts");

			string registryKey = forAllUsers
				? "HKLM:\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Fonts"
				: "HKCU:\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Fonts";

			var psCommand = new StringBuilder("-command \"");

			foreach (string fontFilePath in fontFilePaths)
			{
				var destinationPath = Path.Combine(fontDirectory, Path.GetFileName(fontFilePath));
				var appendCommand = $"Copy-Item {ToPowerShellStringLiteral(fontFilePath)} {ToPowerShellStringLiteral(fontDirectory)}; New-ItemProperty -Name {ToPowerShellStringLiteral(Path.GetFileNameWithoutExtension(fontFilePath))} -Path {ToPowerShellStringLiteral(registryKey)} -PropertyType string -Value {ToPowerShellStringLiteral(destinationPath)};";

				if (psCommand.Length + appendCommand.Length > 32766)
				{
					// The command is too long to run at once, so run the command once up to this point.
					await RunPowershellCommandAsync(psCommand.Append('"').ToString(), PowerShellExecutionOptions.Elevated | PowerShellExecutionOptions.Hidden);
					psCommand.Clear().Append("-command \"");
				}

				psCommand.Append(appendCommand);
			}

			await RunPowershellCommandAsync(psCommand.Append('"').ToString(), PowerShellExecutionOptions.Elevated | PowerShellExecutionOptions.Hidden);
		}

		private static Process CreatePowershellProcess(string command, PowerShellExecutionOptions options, string? workingDirectory = null)
		{
			Process process = new();

			process.StartInfo.FileName = "powershell.exe";
			if (options.HasFlag(PowerShellExecutionOptions.Elevated))
			{
				process.StartInfo.UseShellExecute = true;
				process.StartInfo.Verb = "runas";
			}

			if (options.HasFlag(PowerShellExecutionOptions.Hidden))
			{
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			}

			if (workingDirectory is not null)
				process.StartInfo.WorkingDirectory = workingDirectory;

			process.StartInfo.Arguments = command;

			return process;
		}

		public static SafeFileHandle CreateFileForWrite(string filePath, bool overwrite = true)
		{
			return PInvoke.CreateFile(filePath,
				(uint)FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE, 0, null, overwrite ? FILE_CREATION_DISPOSITION.CREATE_ALWAYS : FILE_CREATION_DISPOSITION.OPEN_ALWAYS, FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS, null);
		}

		public static SafeFileHandle OpenFileForRead(string filePath, bool readWrite = false, uint flags = 0)
		{
			return PInvoke.CreateFile(filePath,
				(uint)FILE_ACCESS_RIGHTS.FILE_GENERIC_READ | (uint)(readWrite ? FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE : 0u), FILE_SHARE_MODE.FILE_SHARE_READ | (readWrite ? 0 : FILE_SHARE_MODE.FILE_SHARE_WRITE), null, FILE_CREATION_DISPOSITION.OPEN_EXISTING, FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS | (FILE_FLAGS_AND_ATTRIBUTES)flags, null);
		}

		public static bool GetFileDateModified(string filePath, out System.Runtime.InteropServices.ComTypes.FILETIME dateModified)
		{
			using var hFile = PInvoke.CreateFile(filePath, (uint)FILE_ACCESS_RIGHTS.FILE_GENERIC_READ, FILE_SHARE_MODE.FILE_SHARE_READ, null, FILE_CREATION_DISPOSITION.OPEN_EXISTING, FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS, null);
			return PInvoke.GetFileTime(hFile, out _, out _, out dateModified);
		}

		public static bool SetFileDateModified(string filePath, System.Runtime.InteropServices.ComTypes.FILETIME dateModified)
		{
			using var hFile = PInvoke.CreateFile(filePath, (uint)FILE_ACCESS_RIGHTS.FILE_WRITE_ATTRIBUTES, 0, null, FILE_CREATION_DISPOSITION.OPEN_EXISTING, FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS, null);
			return PInvoke.SetFileTime(hFile, null, null, dateModified);
		}

		public static FileAttributes GetFileAttributes(string lpFileName)
		{
			uint attributes = PInvoke.GetFileAttributes(lpFileName);
			return attributes == PInvoke.INVALID_FILE_ATTRIBUTES ? FileAttributes.None : (FileAttributes)attributes;
		}

		public static bool HasFileAttribute(string lpFileName, FileAttributes dwAttrs)
		{
			Debug.Assert(dwAttrs != FileAttributes.None);

			return GetFileAttributes(lpFileName).HasFlag(dwAttrs);
		}

		public static bool SetFileAttribute(string lpFileName, FileAttributes dwAttrs)
		{
			uint attributes = PInvoke.GetFileAttributes(lpFileName);
			if (attributes == PInvoke.INVALID_FILE_ATTRIBUTES)
				return false;
			return PInvoke.SetFileAttributes(lpFileName, (FILE_FLAGS_AND_ATTRIBUTES)(attributes | (uint)dwAttrs));
		}

		public static bool UnsetFileAttribute(string lpFileName, FileAttributes dwAttrs)
		{
			uint attributes = PInvoke.GetFileAttributes(lpFileName);
			if (attributes == PInvoke.INVALID_FILE_ATTRIBUTES)
				return false;
			return PInvoke.SetFileAttributes(lpFileName, (FILE_FLAGS_AND_ATTRIBUTES)(attributes & ~(uint)dwAttrs));
		}

		public static unsafe bool CanCompressContent(string path)
		{
			path = Path.GetPathRoot(path) ?? string.Empty;
			uint dwFileSystemFlags = 0;

			var success = PInvoke.GetVolumeInformation(
				path,
				[],
				out _,
				out _,
				out dwFileSystemFlags,
				[]);

			if (!success)
				return false;

			return (dwFileSystemFlags & PInvoke.FILE_FILE_COMPRESSION) != 0;
		}

		public static unsafe bool SetCompressionAttributeIoctl(string lpFileName, bool isCompressed)
		{
			// GENERIC_READ | GENERIC_WRITE flags are needed here
			// FILE_FLAG_BACKUP_SEMANTICS is used to open directories
			using var hFile = PInvoke.CreateFile(
				lpFileName,
				(uint)(FILE_ACCESS_RIGHTS.FILE_GENERIC_READ | FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE | FILE_ACCESS_RIGHTS.FILE_WRITE_ATTRIBUTES),
				FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE,
				lpSecurityAttributes: null,
				FILE_CREATION_DISPOSITION.OPEN_EXISTING,
				FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL | FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS,
				hTemplateFile: null);

			if (hFile.IsInvalid)
				return false;

			var bytesReturned = 0u;
			var compressionFormat = isCompressed
				? COMPRESSION_FORMAT.COMPRESSION_FORMAT_DEFAULT
				: COMPRESSION_FORMAT.COMPRESSION_FORMAT_NONE;

			var result = PInvoke.DeviceIoControl(
				new(hFile.DangerousGetHandle()),
				PInvoke.FSCTL_SET_COMPRESSION,
				&compressionFormat,
				sizeof(ushort),
				null,
				0u,
				&bytesReturned);

			return result;
		}

		public static string? ReadStringFromFile(string filePath)
		{
			try
			{
				using SafeFileHandle hFile = OpenFileForRead(filePath);
				if (hFile.IsInvalid)
					return null;

				using FileStream stream = new(hFile, FileAccess.Read);
				using StreamReader reader = new(stream, detectEncodingFromByteOrderMarks: true);
				return reader.ReadToEnd();
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				return null;
			}
		}

		public static bool WriteStringToFile(string filePath, string str, FILE_FLAGS_AND_ATTRIBUTES flags = 0)
		{
			try
			{
				using SafeFileHandle hStream = PInvoke.CreateFile(filePath,
					(uint)FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE, 0, null, FILE_CREATION_DISPOSITION.CREATE_ALWAYS, FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS | flags, null);
				if (hStream.IsInvalid)
					return false;

				using FileStream stream = new(hStream, FileAccess.Write);
				stream.Write(Encoding.UTF8.GetBytes(str));
				return true;
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				return false;
			}
		}

		// https://www.pinvoke.net/default.aspx/kernel32/GetFileInformationByHandleEx.html
		public static ulong? GetFolderFRN(string? folderPath)
		{
			if (string.IsNullOrEmpty(folderPath))
				return null;

			using var handle = OpenFileForRead(folderPath);
			if (!handle.IsInvalid)
			{
				FILE_ID_BOTH_DIR_INFO fileId = default;
				if (PInvoke.GetFileInformationByHandleEx(handle, FILE_INFO_BY_HANDLE_CLASS.FileIdBothDirectoryInfo, MemoryMarshal.AsBytes(new Span<FILE_ID_BOTH_DIR_INFO>(ref fileId))))
					return (ulong)fileId.FileId;
			}
			return null;
		}

		public static ulong? GetFileFRN(string filePath) => FileIdentity.GetFileFRN(filePath);

		public static long? GetFileSizeOnDisk(string filePath)
		{
			using var handle = OpenFileForRead(filePath);
			if (!handle.IsInvalid)
			{
				try
				{
					FILE_STANDARD_INFO fileInfo = default;
					if (PInvoke.GetFileInformationByHandleEx(handle, FILE_INFO_BY_HANDLE_CLASS.FileStandardInfo, MemoryMarshal.AsBytes(new Span<FILE_STANDARD_INFO>(ref fileInfo))))
						return fileInfo.AllocationSize;
				}
				catch { }
			}
			return null;
		}

		// https://github.com/rad1oactive/BetterExplorer/blob/master/Windows%20API%20Code%20Pack%201.1/source/WindowsAPICodePack/Shell/ReparsePoint.cs
		public static unsafe string? ParseSymLink(string path)
		{
			using var handle = OpenFileForRead(path, false, 0x00200000);
			if (!handle.IsInvalid)
			{
				byte[] buffer = new byte[16 * 1024];
				if (PInvoke.DeviceIoControl(handle, PInvoke.FSCTL_GET_REPARSE_POINT, [], buffer, out uint bytesReturned, null))
				{
					uint reparseTag = MemoryMarshal.Read<uint>(buffer);
					short subsNameOffset = MemoryMarshal.Read<short>(buffer.AsSpan(8));
					short subsNameLength = MemoryMarshal.Read<short>(buffer.AsSpan(10));
					short printNameOffset = MemoryMarshal.Read<short>(buffer.AsSpan(12));
					short printNameLength = MemoryMarshal.Read<short>(buffer.AsSpan(14));
					string pathBuffer = Encoding.Unicode.GetString(buffer, 20, (int)bytesReturned - 20);
					var subsString = pathBuffer.Substring(subsNameOffset / 2, subsNameLength / 2);
					var printString = pathBuffer.Substring(printNameOffset / 2, printNameLength / 2);
					var normalisedTarget = printString ?? subsString;
					if (string.IsNullOrEmpty(normalisedTarget))
					{
						normalisedTarget = subsString;
						if (normalisedTarget.StartsWith(@"\??\", StringComparison.Ordinal))
						{
							normalisedTarget = normalisedTarget.Substring(4);
						}
					}
					if (reparseTag == PInvoke.IO_REPARSE_TAG_SYMLINK && (normalisedTarget.Length < 2 || normalisedTarget[1] != ':'))
					{
						// Target is relative, get the absolute path
						normalisedTarget = normalisedTarget.TrimStart(Path.DirectorySeparatorChar);
						path = path.TrimEnd(Path.DirectorySeparatorChar);
						normalisedTarget = Path.GetFullPath(Path.Combine(path.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar)), normalisedTarget));
					}
					return normalisedTarget;
				}
			}
			return null;
		}

		// https://stackoverflow.com/a/7988352
		public static unsafe IEnumerable<(string Name, long Size)> GetAlternateStreams(string path)
		{
			var streams = new List<(string Name, long Size)>();
			WIN32_FIND_STREAM_DATA findStreamData = default;
			using FindCloseSafeHandle hFile = PInvoke.FindFirstStream(path, STREAM_INFO_LEVELS.FindStreamInfoStandard, &findStreamData);

			if (!hFile.IsInvalid)
			{
				do
				{
					// The documentation for FindFirstStreamW says that it is always a ::$DATA
					// stream type, but FindNextStreamW doesn't guarantee that for subsequent
					// streams so we check to make sure
					var streamName = findStreamData.cStreamName.ToString();
					if (streamName.EndsWith(":$DATA") && streamName != "::$DATA")
						streams.Add((streamName, findStreamData.StreamSize));
				}
				while (PInvoke.FindNextStream(hFile, &findStreamData));
			}

			return streams;
		}

		public static unsafe bool GetWin32FindDataForPath(string targetPath, out WIN32_FIND_DATAW findData)
		{
			fixed (WIN32_FIND_DATAW* findDataPointer = &findData)
			{
				using FindCloseSafeHandle hFile = PInvoke.FindFirstFileEx(
					targetPath,
					FINDEX_INFO_LEVELS.FindExInfoBasic,
					findDataPointer,
					FINDEX_SEARCH_OPS.FindExSearchNameMatch,
					FIND_FIRST_EX_FLAGS.FIND_FIRST_EX_LARGE_FETCH);
				return !hFile.IsInvalid;
			}
		}

		private static string? GetPackageFamilyNameFromAppRegistryName(string appRegistryName)
		{
			using var appXKey = Registry.ClassesRoot.OpenSubKey(appRegistryName + @"\Application");
			var appUserModelIdObj = appXKey?.GetValue("AppUserModelId");
			string? appUserModelId = appUserModelIdObj?.ToString();
			string? packageFamilyName = null;
			if (!string.IsNullOrEmpty(appUserModelId))
			{
				int bangIndex = appUserModelId.IndexOf('!');
				packageFamilyName = bangIndex > 0 ? appUserModelId[..bangIndex] : appUserModelId;
			}

			return packageFamilyName;
		}

		private static string? GetUserChoiceFileAssociation(string filename)
		{
			var fileExtension = Path.GetExtension(filename);
			if (string.IsNullOrEmpty(filename))
				return null;

			try
			{
				// Get ProgId from UserChoice
				using var userChoiceKey = Registry.CurrentUser.OpenSubKey($@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{fileExtension}\UserChoice");
				var progIdObj = userChoiceKey?.GetValue("ProgId");
				string? progId = progIdObj?.ToString();

				if (string.IsNullOrEmpty(progId))
					return null;

				// Get the package family name if it's an AppX app
				if (progId.StartsWith("AppX", StringComparison.OrdinalIgnoreCase))
				{
					string? packageFamilyName = GetPackageFamilyNameFromAppRegistryName(progId);
					if (!string.IsNullOrEmpty(packageFamilyName))
						return packageFamilyName;
				}

				// Find the open command for the ProgId
				using var commandKey = Registry.ClassesRoot.OpenSubKey($@"{progId}\shell\open\command");
				var command = commandKey?.GetValue(null)?.ToString();

				if (string.IsNullOrEmpty(command))
					return null;

				// Extract executable path from command string (e.g. "\"C:\\Program Files\\App\\app.exe\" \"%1\"")
				var exePath = command.Trim();
				if (exePath.StartsWith("\""))
				{
					int endQuote = exePath.IndexOf('\"', 1);
					if (endQuote > 1)
						exePath = exePath.Substring(1, endQuote - 1);
				}
				else
				{
					int firstSpace = exePath.IndexOf(' ');
					if (firstSpace > 0)
						exePath = exePath.Substring(0, firstSpace);
				}

				return File.Exists(exePath) ? exePath : null;
			}
			catch
			{
				return null;
			}
		}

		private static async Task<string?> GetFileAssociationAsync(string filename, bool checkDesktopFirst = true)
		{
			// Find UWP apps
			async Task<string?> GetUwpAssoc()
			{
				var uwpApps = await Launcher.FindFileHandlersAsync(Path.GetExtension(filename));
				return uwpApps.Any() ? uwpApps[0].PackageFamilyName : null;
			}

			// Find desktop apps
			unsafe string? GetDesktopAssoc()
			{
				Span<char> result = stackalloc char[2048];
				fixed (char* fileNamePointer = filename, resultPointer = result)
				{
					var hResult = PInvoke.FindExecutable(fileNamePointer, null, resultPointer);
					int length = result.IndexOf('\0');
					return (nint)(IntPtr)hResult > 32 ? result[..(length < 0 ? result.Length : length)].ToString() : null;
				}
			}

			if (checkDesktopFirst)
				return GetDesktopAssoc() ?? await GetUwpAssoc();

			return await GetUwpAssoc() ?? GetDesktopAssoc();
		}
	}
}

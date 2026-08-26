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

		private static readonly Lock _iconOverlayLock = new();

		/// <summary>
		/// Returns overlay for given file or folder
		/// </summary>
		/// <param name="path"></param>
		/// <param name="isDirectory"></param>
		/// <returns></returns>
		// Maps a target pixel size to a system image list (SHIL_*): 16 -> SMALL, 32 -> LARGE, 48 -> EXTRALARGE, larger -> JUMBO
		private static int GetImageListForSize(int size) => size switch
		{
			<= 16 => 1,
			<= 32 => 0,
			<= 48 => 2,
			_ => 4,
		};

		public static byte[]? GetIconOverlay(string? path, int size, bool isDirectory)
		{
			if (path is null)
				return null;

			SHFILEINFOW shFileInfo = default;
			const SHGFI_FLAGS flags = SHGFI_FLAGS.SHGFI_OVERLAYINDEX | SHGFI_FLAGS.SHGFI_ICON | SHGFI_FLAGS.SHGFI_SYSICONINDEX | SHGFI_FLAGS.SHGFI_ICONLOCATION;
			byte[]? overlayData = null;

			try
			{
				nuint result = PInvoke.SHGetFileInfo(path, isDirectory ? FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_DIRECTORY : 0, ref shFileInfo, flags);
				if (result is 0)
					return null;

				PInvoke.DestroyIcon(shFileInfo.hIcon);

				lock (_iconOverlayLock)
				{
					if (PInvoke.SHGetImageList<IImageList>(GetImageListForSize(size), out var imageList).Failed)
						return null;

					var overlayIdx = shFileInfo.iIcon >> 24;
					if (overlayIdx != 0)
					{
						imageList.GetOverlayImage(overlayIdx, out int overlayImage).ThrowOnFailure();
						imageList.GetIcon(overlayImage, 1, out var overlayHandle).ThrowOnFailure();
						using var hOverlay = overlayHandle;

						if (!hOverlay.IsClosed && !hOverlay.IsInvalid)
						{
							using var image = GetBitmapFromHIcon(hOverlay);
							if (image is null)
								return null;
							overlayData = (byte[]?)new ImageConverter().ConvertTo(image, typeof(byte[]));
						}
					}
				}
			}
			catch (Exception)
			{
				return null;
			}

			return overlayData;
		}

		private static readonly Lock _iconLock = new();

		/// <summary>
		/// Returns an icon if returnIconOnly is true, otherwise a thumbnail will be returned if available.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="size"></param>
		/// <param name="isFolder"></param>
		/// <param name="iconOptions"></param>
		/// <returns></returns>
		public static unsafe byte[]? GetIcon(
			string? path,
			int size,
			bool isFolder,
			IconOptions iconOptions)
		{
			if (string.IsNullOrWhiteSpace(path))
				return null;

			byte[]? iconData = null;

			try
			{
				// Attempt to get file icon/thumbnail using IShellItemImageFactory GetImage
				using var shellItem = SafetyExtensions.IgnoreExceptions(()
					=> ShellFolderExtensions.GetShellItemFromPathOrPIDL(path));

				if (shellItem is not null && shellItem.IShellItem is IShellItemImageFactory shellFactory)
				{
					var flags = SIIGBF.SIIGBF_BIGGERSIZEOK;

					if (iconOptions.HasFlag(IconOptions.ReturnIconOnly))
						flags |= SIIGBF.SIIGBF_ICONONLY;

					if (iconOptions.HasFlag(IconOptions.ReturnThumbnailOnly))
						flags |= SIIGBF.SIIGBF_THUMBNAILONLY;

					if (iconOptions.HasFlag(IconOptions.ReturnOnlyIfCached))
						flags |= SIIGBF.SIIGBF_INCACHEONLY;

					HBITMAP hbitmap = default;
					var hres = shellFactory.GetImage(new(size, size), flags, &hbitmap);
					try
					{
						if (hres == HRESULT.S_OK)
						{
							using var image = GetBitmapFromHBitmap(hbitmap);
							if (image is not null)
								iconData = (byte[]?)new ImageConverter().ConvertTo(image, typeof(byte[]));
						}
					}
					finally
					{
						if (!hbitmap.IsNull)
							PInvoke.DeleteObject(hbitmap);
					}
				}

				if (iconData is not null || iconOptions.HasFlag(IconOptions.ReturnThumbnailOnly))
					return iconData;
				else
				{
					SHFILEINFOW shfi = default;
					const SHGFI_FLAGS flags = SHGFI_FLAGS.SHGFI_OVERLAYINDEX | SHGFI_FLAGS.SHGFI_ICON | SHGFI_FLAGS.SHGFI_SYSICONINDEX | SHGFI_FLAGS.SHGFI_ICONLOCATION | SHGFI_FLAGS.SHGFI_USEFILEATTRIBUTES;

					// Cannot access file, use file attributes
					var ret = PInvoke.SHGetFileInfo(path, isFolder ? FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_DIRECTORY : 0, ref shfi, flags);
					if (ret is 0)
						return iconData;

					PInvoke.DestroyIcon(shfi.hIcon);

					var imageListSize = GetImageListForSize(size);

					lock (_iconLock)
					{
						if (PInvoke.SHGetImageList<IImageList>(imageListSize, out var imageList).Failed)
							return iconData;

						if (iconData is null)
						{
							var iconIdx = shfi.iIcon & 0xFFFFFF;
							if (iconIdx != 0)
							{
								// Could not fetch thumbnail, load simple icon
								imageList.GetIcon(iconIdx, 1, out var iconHandle).ThrowOnFailure();
								using var hIcon = iconHandle;
								if (!hIcon.IsClosed && !hIcon.IsInvalid)
								{
									using (var image = GetBitmapFromHIcon(hIcon))
									{
										if (image is not null)
											iconData = (byte[]?)new ImageConverter().ConvertTo(image, typeof(byte[]));
									}
								}
							}
							else if (isFolder)
							{
								// Could not icon, load generic icon
								var icons = ExtractSelectedIconsFromDLL(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "imageres.dll"), [2], size);
								var generic = icons.SingleOrDefault(x => x.Index == 2);
								iconData = generic?.IconData;
							}
							else
							{
								// Could not icon, load generic icon
								var icons = ExtractSelectedIconsFromDLL(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll"), [1], size);
								var generic = icons.SingleOrDefault(x => x.Index == 1);
								iconData = generic?.IconData;
							}
						}
					}

					return iconData;
				}
			}
			finally
			{

			}
		}

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
		{
			var iconsList = new List<IconFileInfo>();

			foreach (int index in indexes)
			{
				if (_iconCache.TryGetValue((file, index, iconSize), out var iconInfo))
				{
					iconsList.Add(iconInfo);
				}
				else
				{
					// This is merely to pass into the function and is unneeded otherwise
					var result = PInvoke.SHDefExtractIcon(file, -1 * index, 0, out var largeIcon, out var smallIcon, Convert.ToUInt32(iconSize));
					using var icon = largeIcon;
					using var icon2 = smallIcon;
					if (result == HRESULT.S_OK)
					{
						using var image = GetBitmapFromHIcon(icon);
						if (image is null)
							continue;
						byte[] bitmapData = (byte[])(new ImageConverter().ConvertTo(image, typeof(byte[])) ?? Array.Empty<byte>());
						iconInfo = new IconFileInfo(bitmapData, index);
						_iconCache[(file, index, iconSize)] = iconInfo;
						iconsList.Add(iconInfo);
					}
				}
			}

			return iconsList;
		}

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
		}

		public static void SetNetworkDriveLabel(string driveName, string newLabel)
		{
			RunPowershellCommand($"-command \"(New-Object -ComObject Shell.Application).NameSpace({ToPowerShellStringLiteral(driveName)}).Self.Name={ToPowerShellStringLiteral(newLabel)}\"", PowerShellExecutionOptions.Hidden);
		}

		public static Task<bool> MountVhdDisk(string vhdPath)
		{
			// Mounting requires elevation
			return RunPowershellCommandAsync($"-command \"Mount-DiskImage -ImagePath {ToPowerShellStringLiteral(vhdPath)}\"", PowerShellExecutionOptions.Elevated | PowerShellExecutionOptions.Hidden);
		}

		public static Bitmap? GetBitmapFromHBitmap(HBITMAP hBitmap)
		{
			try
			{
				Bitmap bmp = Image.FromHbitmap((IntPtr)hBitmap);
				if (Image.GetPixelFormatSize(bmp.PixelFormat) < 32)
					return bmp;

				Rectangle bmBounds = new Rectangle(0, 0, bmp.Width, bmp.Height);
				var bmpData = bmp.LockBits(bmBounds, ImageLockMode.ReadOnly, bmp.PixelFormat);

				if (IsAlphaBitmap(bmpData))
				{
					var alpha = GetAlphaBitmapFromBitmapData(bmpData);

					bmp.UnlockBits(bmpData);
					bmp.Dispose();

					return alpha;
				}

				bmp.UnlockBits(bmpData);

				return bmp;
			}
			catch
			{
				return null;
			}
		}

		private static Bitmap? GetBitmapFromHIcon(SafeHandle iconHandle)
		{
			if (iconHandle.IsInvalid || iconHandle.IsClosed)
				return null;

			try
			{
				using var icon = (Icon)Icon.FromHandle(iconHandle.DangerousGetHandle()).Clone();
				return icon.ToBitmap();
			}
			catch
			{
				return null;
			}
		}

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

		private static Bitmap GetAlphaBitmapFromBitmapData(BitmapData bmpData)
		{
			using var tmp = new Bitmap(bmpData.Width, bmpData.Height, bmpData.Stride, PixelFormat.Format32bppArgb, bmpData.Scan0);
			Bitmap clone = new Bitmap(tmp.Width, tmp.Height, tmp.PixelFormat);

			using (Graphics gr = Graphics.FromImage(clone))
			{
				gr.DrawImage(tmp, new Rectangle(0, 0, clone.Width, clone.Height));
			}

			return clone;
		}

		private static bool IsAlphaBitmap(BitmapData bmpData)
		{
			for (int y = 0; y <= bmpData.Height - 1; y++)
			{
				for (int x = 0; x <= bmpData.Width - 1; x++)
				{
					Color pixelColor = Color.FromArgb(
						Marshal.ReadInt32(bmpData.Scan0, (bmpData.Stride * y) + (4 * x)));

					if (pixelColor.A < 255)
						return true;
				}
			}

			return false;
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
		{
			string? volumePath = Path.GetPathRoot(volumeHint);
			if (string.IsNullOrEmpty(volumePath))
				return null;

			using var volumeHandle = PInvoke.CreateFile(volumePath, (uint)FILE_ACCESS_RIGHTS.FILE_GENERIC_READ, FILE_SHARE_MODE.FILE_SHARE_READ, null, FILE_CREATION_DISPOSITION.OPEN_EXISTING, FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS, null);
			if (volumeHandle.IsInvalid)
				return null;

			FILE_ID_DESCRIPTOR fileId = new() { dwSize = (uint)Marshal.SizeOf<FILE_ID_DESCRIPTOR>(), Type = FILE_ID_TYPE.FileIdType };
			fileId.FileId = (long)frn;

			using var hFile = PInvoke.OpenFileById(volumeHandle, fileId, (uint)FILE_ACCESS_RIGHTS.FILE_GENERIC_READ, FILE_SHARE_MODE.FILE_SHARE_READ, null, FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS);
			if (hFile.IsInvalid)
				return null;

			Span<char> path = stackalloc char[4096];
			uint length = PInvoke.GetFinalPathNameByHandle(hFile, path, GETFINALPATHNAMEBYHANDLE_FLAGS.FILE_NAME_NORMALIZED | GETFINALPATHNAMEBYHANDLE_FLAGS.VOLUME_NAME_DOS);
			return length is > 0 and < 4096 ? path[..(int)length].ToString() : null;
		}

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
			return new SafeFileHandle(Win32PInvoke.CreateFileFromApp(filePath,
				(uint)FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE, 0, IntPtr.Zero, overwrite ? Win32PInvoke.CREATE_ALWAYS : Win32PInvoke.OPEN_ALWAYS, (uint)Win32PInvoke.File_Attributes.BackupSemantics, IntPtr.Zero), true);
		}

		public static SafeFileHandle OpenFileForRead(string? filePath, bool readWrite = false, uint flags = 0)
		{
			return new SafeFileHandle(Win32PInvoke.CreateFileFromApp(filePath,
				(uint)FILE_ACCESS_RIGHTS.FILE_GENERIC_READ | (uint)(readWrite ? FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE : 0u), (uint)(Win32PInvoke.FILE_SHARE_READ | (readWrite ? 0 : Win32PInvoke.FILE_SHARE_WRITE)), IntPtr.Zero, Win32PInvoke.OPEN_EXISTING, (uint)Win32PInvoke.File_Attributes.BackupSemantics | flags, IntPtr.Zero), true);
		}

		public static bool GetFileDateModified(string filePath, out System.Runtime.InteropServices.ComTypes.FILETIME dateModified)
		{
			using var hFile = new SafeFileHandle(Win32PInvoke.CreateFileFromApp(filePath, (uint)FILE_ACCESS_RIGHTS.FILE_GENERIC_READ, Win32PInvoke.FILE_SHARE_READ, IntPtr.Zero, Win32PInvoke.OPEN_EXISTING, (uint)Win32PInvoke.File_Attributes.BackupSemantics, IntPtr.Zero), true);
			return Win32PInvoke.GetFileTime(hFile.DangerousGetHandle(), out _, out _, out dateModified);
		}

		public static bool SetFileDateModified(string filePath, System.Runtime.InteropServices.ComTypes.FILETIME dateModified)
		{
			using var hFile = new SafeFileHandle(Win32PInvoke.CreateFileFromApp(filePath, (uint)FILE_ACCESS_RIGHTS.FILE_WRITE_ATTRIBUTES, 0, IntPtr.Zero, Win32PInvoke.OPEN_EXISTING, (uint)Win32PInvoke.File_Attributes.BackupSemantics, IntPtr.Zero), true);
			return Win32PInvoke.SetFileTime(hFile.DangerousGetHandle(), new(), new(), dateModified);
		}

		public static FileAttributes GetFileAttributes(string lpFileName)
		{
			if (Win32PInvoke.GetFileAttributesExFromApp(
				lpFileName, Win32PInvoke.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out var lpFileInfo))
			{
				return lpFileInfo.dwFileAttributes;
			}
			return FileAttributes.None;
		}

		public static bool HasFileAttribute(string lpFileName, FileAttributes dwAttrs)
		{
			Debug.Assert(dwAttrs != FileAttributes.None);

			return GetFileAttributes(lpFileName).HasFlag(dwAttrs);
		}

		public static bool SetFileAttribute(string lpFileName, FileAttributes dwAttrs)
		{
			if (!Win32PInvoke.GetFileAttributesExFromApp(
				lpFileName, Win32PInvoke.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out var lpFileInfo))
			{
				return false;
			}
			return Win32PInvoke.SetFileAttributesFromApp(lpFileName, lpFileInfo.dwFileAttributes | dwAttrs);
		}

		public static bool UnsetFileAttribute(string lpFileName, FileAttributes dwAttrs)
		{
			if (!Win32PInvoke.GetFileAttributesExFromApp(
				lpFileName, Win32PInvoke.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out var lpFileInfo))
			{
				return false;
			}
			return Win32PInvoke.SetFileAttributesFromApp(lpFileName, lpFileInfo.dwFileAttributes & ~dwAttrs);
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
			IntPtr hFile = Win32PInvoke.CreateFileFromApp(filePath,
				(uint)FILE_ACCESS_RIGHTS.FILE_GENERIC_READ,
				Win32PInvoke.FILE_SHARE_READ,
				IntPtr.Zero,
				Win32PInvoke.OPEN_EXISTING,
				(uint)Win32PInvoke.File_Attributes.BackupSemantics,
				IntPtr.Zero);

			if (hFile.ToInt64() == -1)
			{
				return null;
			}

			const int BUFFER_LENGTH = 4096;
			byte[] buffer = new byte[BUFFER_LENGTH];
			int dwBytesRead;
			string szRead = string.Empty;

			unsafe
			{
				using (MemoryStream ms = new MemoryStream())
				using (StreamReader reader = new StreamReader(ms, true))
				{
					while (true)
					{
						fixed (byte* pBuffer = buffer)
						{
							if (Win32PInvoke.ReadFile(hFile, pBuffer, BUFFER_LENGTH - 1, &dwBytesRead, IntPtr.Zero) && dwBytesRead > 0)
							{
								ms.Write(buffer, 0, dwBytesRead);
							}
							else
							{
								break;
							}
						}
					}
					ms.Position = 0;
					szRead = reader.ReadToEnd();
				}
			}

			Win32PInvoke.CloseHandle(hFile);

			return szRead;
		}

		public static bool WriteStringToFile(string filePath, string str, Win32PInvoke.File_Attributes flags = 0)
		{
			IntPtr hStream = Win32PInvoke.CreateFileFromApp(filePath,
				(uint)FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE, 0, IntPtr.Zero, Win32PInvoke.CREATE_ALWAYS, (uint)(Win32PInvoke.File_Attributes.BackupSemantics | flags), IntPtr.Zero);
			if (hStream.ToInt64() == -1)
			{
				return false;
			}
			byte[] buff = Encoding.UTF8.GetBytes(str);
			int dwBytesWritten;
			unsafe
			{
				fixed (byte* pBuff = buff)
				{
					Win32PInvoke.WriteFile(hStream, pBuff, buff.Length, &dwBytesWritten, IntPtr.Zero);
				}
			}
			Win32PInvoke.CloseHandle(hStream);
			return true;
		}

		public static bool WriteBufferToFileWithProgress(string filePath, byte[] buffer, Win32PInvoke.LPOVERLAPPED_COMPLETION_ROUTINE callback)
		{
			using var hFile = CreateFileForWrite(filePath);

			if (hFile.IsInvalid)
			{
				return false;
			}

			NativeOverlapped nativeOverlapped = new NativeOverlapped();
			bool result = Win32PInvoke.WriteFileEx(hFile.DangerousGetHandle(), buffer, (uint)buffer.LongLength, ref nativeOverlapped, callback);

			if (!result)
			{
				System.Diagnostics.Debug.WriteLine(Marshal.GetLastWin32Error());
			}

			return result;
		}

		// https://www.pinvoke.net/default.aspx/kernel32/GetFileInformationByHandleEx.html
		public static ulong? GetFolderFRN(string? folderPath)
		{
			using var handle = OpenFileForRead(folderPath);
			if (!handle.IsInvalid)
			{
				var fileStruct = new Win32PInvoke.FILE_ID_BOTH_DIR_INFO();
				if (Win32PInvoke.GetFileInformationByHandleEx(handle.DangerousGetHandle(), Win32PInvoke.FILE_INFO_BY_HANDLE_CLASS.FileIdBothDirectoryInfo, out fileStruct, (uint)Marshal.SizeOf(fileStruct)))
				{
					return (ulong)fileStruct.FileId;
				}
			}
			return null;
		}

		public static unsafe ulong? GetFileFRN(string filePath)
		{
			using var handle = OpenFileForRead(filePath);
			if (!handle.IsInvalid)
			{
				try
				{
					FILE_ID_INFO fileId = default;
					if (PInvoke.GetFileInformationByHandleEx(handle, FILE_INFO_BY_HANDLE_CLASS.FileIdInfo, MemoryMarshal.AsBytes(new Span<FILE_ID_INFO>(ref fileId))))
						return BitConverter.ToUInt64(fileId.FileId.Identifier.AsReadOnlySpan());
				}
				catch { }
			}
			return null;
		}

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
		public static string? ParseSymLink(string path)
		{
			using var handle = OpenFileForRead(path, false, 0x00200000);
			if (!handle.IsInvalid)
			{
				if (Win32PInvoke.DeviceIoControl(handle.DangerousGetHandle(), Win32PInvoke.FSCTL_GET_REPARSE_POINT, IntPtr.Zero, 0, out Win32PInvoke.REPARSE_DATA_BUFFER buffer, Win32PInvoke.MAXIMUM_REPARSE_DATA_BUFFER_SIZE, out _, IntPtr.Zero))
				{
					var subsString = new string(buffer.PathBuffer, ((buffer.SubsNameOffset / 2) + 2), buffer.SubsNameLength / 2);
					var printString = new string(buffer.PathBuffer, ((buffer.PrintNameOffset / 2) + 2), buffer.PrintNameLength / 2);
					var normalisedTarget = printString ?? subsString;
					if (string.IsNullOrEmpty(normalisedTarget))
					{
						normalisedTarget = subsString;
						if (normalisedTarget.StartsWith(@"\??\", StringComparison.Ordinal))
						{
							normalisedTarget = normalisedTarget.Substring(4);
						}
					}
					if (buffer.ReparseTag == Win32PInvoke.IO_REPARSE_TAG_SYMLINK && (normalisedTarget.Length < 2 || normalisedTarget[1] != ':'))
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
		public static IEnumerable<(string Name, long Size)> GetAlternateStreams(string path)
		{
			Win32PInvoke.WIN32_FIND_STREAM_DATA findStreamData = new Win32PInvoke.WIN32_FIND_STREAM_DATA();
			IntPtr hFile = Win32PInvoke.FindFirstStreamW(path, Win32PInvoke.StreamInfoLevels.FindStreamInfoStandard, findStreamData, 0);

			if (hFile.ToInt64() != -1)
			{
				do
				{
					// The documentation for FindFirstStreamW says that it is always a ::$DATA
					// stream type, but FindNextStreamW doesn't guarantee that for subsequent
					// streams so we check to make sure
					var streamName = findStreamData.cStreamName
						?? throw new InvalidDataException("The alternate-stream enumeration returned an item without a name.");
					if (streamName.EndsWith(":$DATA") && streamName != "::$DATA")
					{
						yield return (streamName, findStreamData.StreamSize);
					}
				}
				while (Win32PInvoke.FindNextStreamW(hFile, findStreamData));

				Win32PInvoke.FindClose(hFile);
			}
		}

		public static bool GetWin32FindDataForPath(string targetPath, out Win32PInvoke.WIN32_FIND_DATA findData)
		{
			Win32PInvoke.FINDEX_INFO_LEVELS findInfoLevel = Win32PInvoke.FINDEX_INFO_LEVELS.FindExInfoBasic;

			int additionalFlags = Win32PInvoke.FIND_FIRST_EX_LARGE_FETCH;

			IntPtr hFile = Win32PInvoke.FindFirstFileExFromApp(
				targetPath,
				findInfoLevel,
				out findData,
				Win32PInvoke.FINDEX_SEARCH_OPS.FindExSearchNameMatch,
				IntPtr.Zero,
				additionalFlags);

			if (hFile.ToInt64() != -1)
			{
				Win32PInvoke.FindClose(hFile);

				return true;
			}

			return false;
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

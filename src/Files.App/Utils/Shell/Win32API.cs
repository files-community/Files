// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Vanara.PInvoke;
using Windows.System;

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Provides static helper for general Win32API.
	/// </summary>
	internal class Win32API
	{
		public static Task StartSTATask(Func<Task> func)
		{
			var taskCompletionSource = new TaskCompletionSource();
			Thread thread = new Thread(async () =>
			{
				Ole32.OleInitialize();

				try
				{
					await func();
					taskCompletionSource.SetResult();
				}
				catch (Exception ex)
				{
					taskCompletionSource.SetResult();
					App.Logger.LogWarning(ex, ex.Message);
				}
				finally
				{
					Ole32.OleUninitialize();
				}
			})

			{
				IsBackground = true,
				Priority = ThreadPriority.Normal
			};

			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();

			return taskCompletionSource.Task;
		}

		public static Task StartSTATask(Action action)
		{
			var taskCompletionSource = new TaskCompletionSource();
			Thread thread = new Thread(() =>
			{
				Ole32.OleInitialize();

				try
				{
					action();
					taskCompletionSource.SetResult();
				}
				catch (Exception ex)
				{
					taskCompletionSource.SetResult();
					App.Logger.LogWarning(ex, ex.Message);
				}
				finally
				{
					Ole32.OleUninitialize();
				}
			})

			{
				IsBackground = true,
				Priority = ThreadPriority.Normal
			};

			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();

			return taskCompletionSource.Task;
		}

		public static Task<T?> StartSTATask<T>(Func<T> func)
		{
			var taskCompletionSource = new TaskCompletionSource<T?>();

			Thread thread = new Thread(() =>
			{
				Ole32.OleInitialize();

				try
				{
					taskCompletionSource.SetResult(func());
				}
				catch (Exception ex)
				{
					taskCompletionSource.SetResult(default);
					App.Logger.LogWarning(ex, ex.Message);
					//tcs.SetException(e);
				}
				finally
				{
					Ole32.OleUninitialize();
				}
			})

			{
				IsBackground = true,
				Priority = ThreadPriority.Normal
			};

			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();

			return taskCompletionSource.Task;
		}

		public static Task<T?> StartSTATask<T>(Func<Task<T>> func)
		{
			var taskCompletionSource = new TaskCompletionSource<T?>();

			Thread thread = new Thread(async () =>
			{
				Ole32.OleInitialize();
				try
				{
					taskCompletionSource.SetResult(await func());
				}
				catch (Exception ex)
				{
					taskCompletionSource.SetResult(default);
					App.Logger.LogInformation(ex, ex.Message);
					//tcs.SetException(e);
				}
				finally
				{
					Ole32.OleUninitialize();
				}
			})

			{
				IsBackground = true,
				Priority = ThreadPriority.Normal
			};

			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();

			return taskCompletionSource.Task;
		}

		public static async Task<string?> GetFileAssociationAsync(string filename, bool checkDesktopFirst = false)
		{
			// Find UWP apps
			async Task<string?> GetUwpAssoc()
			{
				var uwpApps = await Launcher.FindFileHandlersAsync(Path.GetExtension(filename));
				return uwpApps.Any() ? uwpApps[0].PackageFamilyName : null;
			}

			// Find desktop apps
			string? GetDesktopAssoc()
			{
				var lpResult = new StringBuilder(2048);
				var hResult = Shell32.FindExecutable(filename, null, lpResult);

				return hResult.ToInt64() > 32 ? lpResult.ToString() : null;
			}

			if (checkDesktopFirst)
				return GetDesktopAssoc() ?? await GetUwpAssoc();

			return await GetUwpAssoc() ?? GetDesktopAssoc();
		}

		public static string ExtractStringFromDLL(string file, int number)
		{
			var lib = Kernel32.LoadLibrary(file);
			StringBuilder result = new StringBuilder(2048);

			_ = User32.LoadString(lib, number, result, result.Capacity);
			Kernel32.FreeLibrary(lib);

			return result.ToString();
		}

		public static string?[] CommandLineToArgs(string commandLine)
		{
			if (string.IsNullOrEmpty(commandLine))
				return Array.Empty<string>();

			var argv = Shell32.CommandLineToArgvW(commandLine, out int argc);
			if (argv == IntPtr.Zero)
				throw new Win32Exception();

			try
			{
				var args = new string?[argc];
				for (var i = 0; i < args.Length; i++)
				{
					var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
					args[i] = Marshal.PtrToStringUni(p);
				}

				return args;
			}
			finally
			{
				Marshal.FreeHGlobal(argv);
			}
		}

		private class IconAndOverlayCacheEntry
		{
			public byte[]? Icon { get; set; }

			public byte[]? Overlay { get; set; }
		}

		private static readonly ConcurrentDictionary<string, ConcurrentDictionary<int, IconAndOverlayCacheEntry>> _iconAndOverlayCache = new();

		private static readonly object _lock = new object();

		public static (byte[]? icon, byte[]? overlay) GetFileIconAndOverlay(string path, int thumbnailSize, bool isFolder, bool getOverlay = true, bool onlyGetOverlay = false)
		{
			byte[]? iconData = null, overlayData = null;
			var entry = _iconAndOverlayCache.GetOrAdd(path, _ => new());

			if (entry.TryGetValue(thumbnailSize, out var cacheEntry))
			{
				iconData = cacheEntry.Icon;
				overlayData = cacheEntry.Overlay;

				if ((onlyGetOverlay && overlayData is not null) ||
					(!getOverlay && iconData is not null) ||
					(overlayData is not null && iconData is not null))
				{
					return (iconData, overlayData);
				}
			}

			try
			{
				if (!onlyGetOverlay)
				{
					using var shellItem = SafetyExtensions.IgnoreExceptions(()
						=> ShellFolderExtensions.GetShellItemFromPathOrPIDL(path));

					if (shellItem is not null && shellItem.IShellItem is Shell32.IShellItemImageFactory fctry)
					{
						var flags = Shell32.SIIGBF.SIIGBF_BIGGERSIZEOK;
						if (thumbnailSize < 80) flags |= Shell32.SIIGBF.SIIGBF_ICONONLY;

						var hres = fctry.GetImage(new SIZE(thumbnailSize, thumbnailSize), flags, out var hbitmap);
						if (hres == HRESULT.S_OK)
						{
							using var image = GetBitmapFromHBitmap(hbitmap);
							if (image is not null)
								iconData = (byte[]?)new ImageConverter().ConvertTo(image, typeof(byte[]));
						}

						//Marshal.ReleaseComObject(fctry);
					}
				}

				if (getOverlay || (!onlyGetOverlay && iconData is null))
				{
					var shfi = new Shell32.SHFILEINFO();
					var flags = Shell32.SHGFI.SHGFI_OVERLAYINDEX | Shell32.SHGFI.SHGFI_ICON | Shell32.SHGFI.SHGFI_SYSICONINDEX | Shell32.SHGFI.SHGFI_ICONLOCATION;

					// Cannot access file, use file attributes
					var useFileAttibutes = !onlyGetOverlay && iconData is null;

					var ret = ShellFolderExtensions.GetStringAsPIDL(path, out var pidl) ?
						Shell32.SHGetFileInfo(pidl, 0, ref shfi, Shell32.SHFILEINFO.Size, Shell32.SHGFI.SHGFI_PIDL | flags) :
						Shell32.SHGetFileInfo(path, isFolder ? FileAttributes.Directory : 0, ref shfi, Shell32.SHFILEINFO.Size, flags | (useFileAttibutes ? Shell32.SHGFI.SHGFI_USEFILEATTRIBUTES : 0));
					if (ret == IntPtr.Zero)
						return (iconData, null);

					User32.DestroyIcon(shfi.hIcon);

					var imageListSize = thumbnailSize switch
					{
						<= 16 => Shell32.SHIL.SHIL_SMALL,
						<= 32 => Shell32.SHIL.SHIL_LARGE,
						<= 48 => Shell32.SHIL.SHIL_EXTRALARGE,
						_ => Shell32.SHIL.SHIL_JUMBO,
					};

					lock (_lock)
					{
						if (!Shell32.SHGetImageList(imageListSize, typeof(ComCtl32.IImageList).GUID, out var imageListOut).Succeeded)
							return (iconData, null);

						var imageList = (ComCtl32.IImageList)imageListOut;

						if (!onlyGetOverlay && iconData is null)
						{
							var iconIdx = shfi.iIcon & 0xFFFFFF;
							if (iconIdx != 0)
							{
								// Could not fetch thumbnail, load simple icon
								using var hIcon = imageList.GetIcon(iconIdx, ComCtl32.IMAGELISTDRAWFLAGS.ILD_TRANSPARENT);
								if (!hIcon.IsNull && !hIcon.IsInvalid)
								{
									using (var icon = hIcon.ToIcon())
									using (var image = icon.ToBitmap())
									{
										iconData = (byte[]?)new ImageConverter().ConvertTo(image, typeof(byte[]));
									}
								}
							}
							else if (isFolder)
							{
								// Could not icon, load generic icon
								var icons = ExtractSelectedIconsFromDLL(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "imageres.dll"), new[] { 2 }, thumbnailSize);
								var generic = icons.SingleOrDefault(x => x.Index == 2);
								iconData = generic?.IconData;
							}
							else
							{
								// Could not icon, load generic icon
								var icons = ExtractSelectedIconsFromDLL(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll"), new[] { 1 }, thumbnailSize);
								var generic = icons.SingleOrDefault(x => x.Index == 1);
								iconData = generic?.IconData;
							}
						}

						var overlayIdx = shfi.iIcon >> 24;
						if (overlayIdx != 0 && getOverlay)
						{
							var overlayImage = imageList.GetOverlayImage(overlayIdx);
							using var hOverlay = imageList.GetIcon(overlayImage, ComCtl32.IMAGELISTDRAWFLAGS.ILD_TRANSPARENT);
							if (!hOverlay.IsNull && !hOverlay.IsInvalid)
							{
								using var icon = hOverlay.ToIcon();
								using var image = icon.ToBitmap();

								overlayData = (byte[]?)new ImageConverter().ConvertTo(image, typeof(byte[]));
							}
						}

						Marshal.ReleaseComObject(imageList);
					}

					return (iconData, overlayData);
				}
				else
				{
					return (iconData, null);
				}
			}
			finally
			{
				cacheEntry = new IconAndOverlayCacheEntry();
				if (iconData is not null)
					cacheEntry.Icon = iconData;

				if (overlayData is not null)
					cacheEntry.Overlay = overlayData;

				entry[thumbnailSize] = cacheEntry;
			}
		}

		public static async Task<bool> RunPowershellCommandAsync(string command, bool runAsAdmin)
		{
			using Process process = CreatePowershellProcess(command, runAsAdmin);
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
			catch (Win32Exception)
			{
				// If user cancels UAC
				return false;
			}
		}

		public static bool RunPowershellCommand(string command, bool runAsAdmin)
		{
			try
			{
				using Process process = CreatePowershellProcess(command, runAsAdmin);

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
					if (Shell32.SHDefExtractIcon(file, -1 * index, 0, out User32.SafeHICON icon, out User32.SafeHICON hIcon2, Convert.ToUInt32(iconSize)) == HRESULT.S_OK)
					{
						using var image = icon.ToBitmap();
						byte[] bitmapData = (byte[])(new ImageConverter().ConvertTo(image, typeof(byte[])) ?? Array.Empty<byte>());
						iconInfo = new IconFileInfo(bitmapData, index);
						_iconCache[(file, index, iconSize)] = iconInfo;
						iconsList.Add(iconInfo);
						User32.DestroyIcon(icon);
						User32.DestroyIcon(hIcon2);
					}
				}
			}

			return iconsList;
		}

		public static IList<IconFileInfo>? ExtractIconsFromDLL(string file)
		{
			var iconsList = new List<IconFileInfo>();
			using var currentProc = Process.GetCurrentProcess();

			using var icoCnt = Shell32.ExtractIcon(currentProc.Handle, file, -1);
			if (icoCnt is null)
				return null;

			int count = icoCnt.DangerousGetHandle().ToInt32();
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
					using var icon = Shell32.ExtractIcon(currentProc.Handle, file, i);
					using var image = icon.ToBitmap();

					byte[] bitmapData = (byte[])(new ImageConverter().ConvertTo(image, typeof(byte[])) ?? Array.Empty<byte>());
					iconInfo = new IconFileInfo(bitmapData, i);
					_iconCache[(file, i, -1)] = iconInfo;
					iconsList.Add(iconInfo);
				}
			}

			return iconsList;
		}

		public static bool SetCustomDirectoryIcon(string? folderPath, string? iconFile, int iconIndex = 0)
		{
			if (folderPath is null)
				return false;

			var fcs = new Shell32.SHFOLDERCUSTOMSETTINGS()
			{
				dwMask = Shell32.FOLDERCUSTOMSETTINGSMASK.FCSM_ICONFILE,
				pszIconFile = iconFile,
				cchIconFile = 0,
				iIconIndex = iconIndex,
			};

			fcs.dwSize = (uint)Marshal.SizeOf(fcs);

			var success = Shell32.SHGetSetFolderCustomSettings(ref fcs, folderPath, Shell32.FCS.FCS_FORCEWRITE).Succeeded;
			if (success)
				_iconAndOverlayCache[folderPath] = new();

			return success;
		}

		public static bool SetCustomFileIcon(string? filePath, string? iconFile, int iconIndex = 0)
		{
			if (filePath is null)
				return false;

			var success = FileOperationsHelpers.SetLinkIcon(filePath, iconFile, iconIndex);
			if (success)
				_iconAndOverlayCache[filePath] = new();

			return success;
		}

		public static Task OpenFormatDriveDialog(string drive)
		{
			// Format requires elevation
			int driveIndex = drive.ToUpperInvariant()[0] - 'A';
			return RunPowershellCommandAsync($"-command \"$Signature = '[DllImport(\\\"shell32.dll\\\", SetLastError = false)]public static extern uint SHFormatDrive(IntPtr hwnd, uint drive, uint fmtID, uint options);'; $SHFormatDrive = Add-Type -MemberDefinition $Signature -Name \"Win32SHFormatDrive\" -Namespace Win32Functions -PassThru; $SHFormatDrive::SHFormatDrive(0, {driveIndex}, 0xFFFF, 0x0001)\"", true);
		}

		public static void SetVolumeLabel(string drivePath, string newLabel)
		{
			// Rename requires elevation
			RunPowershellCommand($"-command \"$Signature = '[DllImport(\\\"kernel32.dll\\\", SetLastError = false)]public static extern bool SetVolumeLabel(string lpRootPathName, string lpVolumeName);'; $SetVolumeLabel = Add-Type -MemberDefinition $Signature -Name \"Win32SetVolumeLabel\" -Namespace Win32Functions -PassThru; $SetVolumeLabel::SetVolumeLabel('{drivePath}', '{newLabel}')\"", true);
		}

		public static void SetNetworkDriveLabel(string driveName, string newLabel)
		{
			RunPowershellCommand($"-command \"(New-Object -ComObject Shell.Application).NameSpace('{driveName}').Self.Name='{newLabel}'\"", false);
		}

		public static Task<bool> MountVhdDisk(string vhdPath)
		{
			// Mounting requires elevation
			return RunPowershellCommandAsync($"-command \"Mount-DiskImage -ImagePath '{vhdPath}'\"", true);
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

		public static Shell32.ITaskbarList4? CreateTaskbarObject()
		{
			try
			{
				var taskbar2 = new Shell32.ITaskbarList2();
				taskbar2.HrInit();
				return taskbar2 as Shell32.ITaskbarList4;
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

					if (pixelColor.A > 0 & pixelColor.A < 255)
						return true;
				}
			}

			return false;
		}

		// There is usually no need to define Win32 COM interfaces/P-Invoke methods here.
		// The Vanara library contains the definitions for all members of Shell32.dll, User32.dll and more
		// The ones below are due to bugs in the current version of the library and can be removed once fixed
		// Structure used by SHQueryRecycleBin.
		[StructLayout(LayoutKind.Sequential, Pack = 0)]
		public struct SHQUERYRBINFO
		{
			public int cbSize;

			public long i64Size;

			public long i64NumItems;
		}

		public static IEnumerable<HWND> GetDesktopWindows()
		{
			HWND prevHwnd = HWND.NULL;
			var windowsList = new List<HWND>();

			while (true)
			{
				prevHwnd = User32.FindWindowEx(HWND.NULL, prevHwnd, null, null);
				if (prevHwnd == HWND.NULL)
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

					var newWindows = GetDesktopWindows().Except(currentWindows).Where(x => User32.IsWindowVisible(x) && !User32.IsIconic(x));
					if (newWindows.Any())
					{
						foreach (var newWindow in newWindows)
						{
							User32.SetWindowPos(
								newWindow,
								User32.SpecialWindowHandles.HWND_TOPMOST,
								0, 0, 0, 0,
								User32.SetWindowPosFlags.SWP_NOSIZE | User32.SetWindowPosFlags.SWP_NOMOVE);

							User32.SetWindowPos(
								newWindow,
								User32.SpecialWindowHandles.HWND_NOTOPMOST,
								0, 0, 0, 0,
								User32.SetWindowPosFlags.SWP_SHOWWINDOW | User32.SetWindowPosFlags.SWP_NOSIZE | User32.SetWindowPosFlags.SWP_NOMOVE);
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

			using var volumeHandle = Kernel32.CreateFile(volumePath, Kernel32.FileAccess.GENERIC_READ, FileShare.Read, null, FileMode.Open, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS);
			if (volumeHandle.IsInvalid)
				return null;

			var fileId = new Kernel32.FILE_ID_DESCRIPTOR() { Type = 0, Id = new Kernel32.FILE_ID_DESCRIPTOR.DUMMYUNIONNAME() { FileId = (long)frn } };
			fileId.dwSize = (uint)Marshal.SizeOf(fileId);

			using var hFile = Kernel32.OpenFileById(volumeHandle, fileId, Kernel32.FileAccess.GENERIC_READ, FileShare.Read, null, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS);
			if (hFile.IsInvalid)
				return null;

			var sb = new StringBuilder(4096);
			var ret = Kernel32.GetFinalPathNameByHandle(hFile, sb, 4095, 0);

			return (ret != 0) ? sb.ToString() : null;
		}

		public class Win32Window : IWin32Window
		{
			public IntPtr Handle { get; set; }

			public static Win32Window FromLong(long hwnd)
				=> new Win32Window() { Handle = new IntPtr(hwnd) };
		}

		public static void OpenFolderInExistingShellWindow(string folderPath)
		{
			var opened = false;

			if (Ole32.CoCreateInstance(typeof(Shell32.ShellWindows).GUID, null, Ole32.CLSCTX.CLSCTX_LOCAL_SERVER, typeof(Shell32.IShellWindows).GUID, out var shellWindowsUnk).Succeeded)
			{
				var shellWindows = (Shell32.IShellWindows)shellWindowsUnk;

				using var controlPanelCategoryView = new Vanara.Windows.Shell.ShellItem("::{26EE0668-A00A-44D7-9371-BEB064C98683}");

				for (int i = 0; i < shellWindows.Count; i++)
				{
					var item = shellWindows.Item(i);

					var serv = (Shell32.IServiceProvider)item;
					if (serv is not null)
					{
						if (serv.QueryService(Shell32.SID_STopLevelBrowser, typeof(Shell32.IShellBrowser).GUID, out var ppv).Succeeded)
						{
							var pUnk = Marshal.GetObjectForIUnknown(ppv);
							var shellBrowser = (Shell32.IShellBrowser)pUnk;

							using var targetFolder = SafetyExtensions.IgnoreExceptions(() => new Vanara.Windows.Shell.ShellItem(folderPath));
							if (targetFolder is not null)
							{
								if (shellBrowser.QueryActiveShellView(out var shellView).Succeeded)
								{
									var folderView = (Shell32.IFolderView)shellView;
									var folder = folderView.GetFolder<Shell32.IPersistFolder2>();
									var folderPidl = new Shell32.PIDL(IntPtr.Zero);

									if (folder.GetCurFolder(ref folderPidl).Succeeded)
									{
										if (folderPidl.IsParentOf(targetFolder.PIDL.DangerousGetHandle(), true) ||
											folderPidl.Equals(controlPanelCategoryView.PIDL))
										{
											if (shellBrowser.BrowseObject(targetFolder.PIDL.DangerousGetHandle(), Shell32.SBSP.SBSP_SAMEBROWSER | Shell32.SBSP.SBSP_ABSOLUTE).Succeeded)
											{
												opened = true;

												break;
											}
										}
									}

									folderPidl.Dispose();

									Marshal.ReleaseComObject(folder);
									Marshal.ReleaseComObject(folderView);
									Marshal.ReleaseComObject(shellView);
								}
							}

							Marshal.ReleaseComObject(shellBrowser);
							Marshal.ReleaseComObject(pUnk);
						}

						Marshal.ReleaseComObject(serv);
					}

					Marshal.ReleaseComObject(item);
				}

				Marshal.ReleaseComObject(shellWindows);
				Marshal.ReleaseComObject(shellWindowsUnk);
			}

			if (!opened)
			{
				Shell32.ShellExecute(
					HWND.NULL,
					"open",
					Environment.ExpandEnvironmentVariables("%windir%\\explorer.exe"),
					folderPath,
					null,
					ShowWindowCommand.SW_SHOWNORMAL);
			}
		}

		// Get information from recycle bin.
		[DllImport(Lib.Shell32, SetLastError = false, CharSet = CharSet.Unicode)]
		public static extern int SHQueryRecycleBin(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

		public static async Task<bool> InstallInf(string filePath)
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

		public static Task InstallFont(string fontFilePath, bool forAllUsers)
		{
			string fontDirectory = forAllUsers
				? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts")
				: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Fonts");

			string registryKey = forAllUsers
				? "HKLM:\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Fonts"
				: "HKCU:\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Fonts";

			var destinationPath = Path.Combine(fontDirectory, Path.GetFileName(fontFilePath));

			return RunPowershellCommandAsync($"-command \"Copy-Item '{fontFilePath}' '{fontDirectory}'; New-ItemProperty -Name '{Path.GetFileNameWithoutExtension(fontFilePath)}' -Path '{registryKey}' -PropertyType string -Value '{destinationPath}'\"", forAllUsers);
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
				var appendCommand = $"Copy-Item '{fontFilePath}' '{fontDirectory}'; New-ItemProperty -Name '{Path.GetFileNameWithoutExtension(fontFilePath)}' -Path '{registryKey}' -PropertyType string -Value '{destinationPath}';";

				if (psCommand.Length + appendCommand.Length > 32766)
				{
					// The command is too long to run at once, so run the command once up to this point.
					await RunPowershellCommandAsync(psCommand.Append("\"").ToString(), forAllUsers);
					psCommand.Clear().Append("-command \"");
				}

				psCommand.Append(appendCommand);
			}

			await RunPowershellCommandAsync(psCommand.Append("\"").ToString(), forAllUsers);
		}

		private static Process CreatePowershellProcess(string command, bool runAsAdmin)
		{
			Process process = new();

			if (runAsAdmin)
			{
				process.StartInfo.UseShellExecute = true;
				process.StartInfo.Verb = "runas";
			}

			process.StartInfo.FileName = "powershell.exe";
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo.Arguments = command;

			return process;
		}
	}
}

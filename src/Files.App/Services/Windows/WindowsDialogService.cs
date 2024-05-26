// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Vanara.Extensions;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace Files.App.Services
{
	/// <inheritdoc cref="ICommonDialogService"/>
	public sealed class CommonDialogService : ICommonDialogService
	{
		/// <inheritdoc/>
		public bool Open_FileOpenDialog(nint hWnd, bool pickFoldersOnly, string[] filters, Environment.SpecialFolder defaultFolder, out string filePath)
		{
			filePath = string.Empty;

			try
			{
				unsafe
				{
					// Get a new instance of the dialog
					PInvoke.CoCreateInstance(
						typeof(FileOpenDialog).GUID,
						null,
						CLSCTX.CLSCTX_INPROC_SERVER,
						out IFileOpenDialog dialog)
					.ThrowOnFailure();

					if (filters.Length is not 0 && filters.Length % 2 is 0)
					{
						List<COMDLG_FILTERSPEC> extensions = [];

						for (int i = 1; i < filters.Length; i += 2)
						{
							COMDLG_FILTERSPEC extension;

							extension.pszSpec = (char*)Marshal.StringToHGlobalUni(filters[i]);
							extension.pszName = (char*)Marshal.StringToHGlobalUni(filters[i - 1]);

							// Add to the exclusive extension list
							extensions.Add(extension);
						}

						// Set the file type using the extension list
						dialog.SetFileTypes(extensions.ToArray());
					}

					// Get the default shell folder (My Computer)
					PInvoke.SHCreateItemFromParsingName(
						Environment.GetFolderPath(defaultFolder),
						null,
						typeof(IShellItem).GUID,
						out var directoryShellItem)
					.ThrowOnFailure();

					// Folder picker
					if (pickFoldersOnly)
					{
						dialog.SetOptions(FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS);
					}

					// Set the default folder to open in the dialog
					dialog.SetFolder((IShellItem)directoryShellItem);
					dialog.SetDefaultFolder((IShellItem)directoryShellItem);

					// Show the dialog
					dialog.Show(new HWND(hWnd));

					// Get the file that user chose
					dialog.GetResult(out var resultShellItem);
					resultShellItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var lpFilePath);
					filePath = lpFilePath.ToString();

					return true;
				}
			}
			catch (Exception ex)
			{
				App.Logger.LogError(ex, "Failed to open a common dialog called FileOpenDialog.");

				return false;
			}
		}

		/// <inheritdoc/>
		public bool Open_FileSaveDialog(nint hWnd, bool pickFoldersOnly, string[] filters, Environment.SpecialFolder defaultFolder, out string filePath)
		{
			filePath = string.Empty;

			try
			{
				unsafe
				{
					// Get a new instance of the dialog
					PInvoke.CoCreateInstance(
						typeof(FileSaveDialog).GUID,
						null,
						CLSCTX.CLSCTX_INPROC_SERVER,
						out IFileSaveDialog dialog)
					.ThrowOnFailure();

					if (filters.Length is not 0 && filters.Length % 2 is 0)
					{
						List<COMDLG_FILTERSPEC> extensions = [];

						for (int i = 1; i < filters.Length; i += 2)
						{
							COMDLG_FILTERSPEC extension;

							extension.pszSpec = (char*)Marshal.StringToHGlobalUni(filters[i]);
							extension.pszName = (char*)Marshal.StringToHGlobalUni(filters[i - 1]);

							// Add to the exclusive extension list
							extensions.Add(extension);
						}

						// Set the file type using the extension list
						dialog.SetFileTypes(extensions.ToArray());
					}

					// Get the default shell folder (My Computer)
					PInvoke.SHCreateItemFromParsingName(
						Environment.GetFolderPath(defaultFolder),
						null,
						typeof(IShellItem).GUID,
						out var directoryShellItem)
					.ThrowOnFailure();

					// Folder picker
					if (pickFoldersOnly)
						dialog.SetOptions(FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS);

					// Set the default folder to open in the dialog
					dialog.SetFolder((IShellItem)directoryShellItem);
					dialog.SetDefaultFolder((IShellItem)directoryShellItem);

					// Show the dialog
					dialog.Show(new HWND(hWnd));

					// Get the file that user chose
					dialog.GetResult(out var resultShellItem);
					resultShellItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var lpFilePath);
					filePath = lpFilePath.ToString();

					return true;
				}
			}
			catch (Exception ex)
			{
				App.Logger.LogError(ex, "Failed to open a common dialog called FileSaveDialog.");

				return false;
			}
		}

		/// <inheritdoc/>
		public bool Open_NetworkConnectionDialog(nint hWind, bool hideRestoreConnectionCheckBox = false, bool persistConnectionAtLogon = false, bool readOnlyPath = false, string? remoteNetworkName = null, bool useMostRecentPath = false)
		{
			using var dialog = new NetworkConnectionDialog()
			{
				HideRestoreConnectionCheckBox = hideRestoreConnectionCheckBox,
				PersistConnectionAtLogon = persistConnectionAtLogon,
				ReadOnlyPath = readOnlyPath,
				RemoteNetworkName = remoteNetworkName!,
				UseMostRecentPath = useMostRecentPath,
			};

			var window = Win32Helper.Win32Window.FromLong(hWind.ToInt64());

			return dialog.ShowDialog(window) == System.Windows.Forms.DialogResult.OK;
		}

		private sealed class NetworkConnectionDialog : CommonDialog
		{
			private readonly Vanara.PInvoke.Mpr.NETRESOURCE netRes = new();
			private Vanara.PInvoke.Mpr.CONNECTDLGSTRUCT dialogOptions;

			/// <summary>
			/// Initializes a new instance of the <see cref="NetworkConnectionDialog"/> class.
			/// </summary>
			public NetworkConnectionDialog()
			{
				dialogOptions.cbStructure = (uint)Marshal.SizeOf(typeof(Vanara.PInvoke.Mpr.CONNECTDLGSTRUCT));
				netRes.dwType = Vanara.PInvoke.Mpr.NETRESOURCEType.RESOURCETYPE_DISK;
			}

			/// <summary>Gets the connected device number. This value is only valid after successfully running the dialog.</summary>
			/// <value>The connected device number. The value is 1 for A:, 2 for B:, 3 for C:, and so on. If the user made a deviceless connection, the value is –1.</value>
			[Browsable(false)]
			public int ConnectedDeviceNumber
				=> dialogOptions.dwDevNum;

			/// <summary>Gets or sets a value indicating whether to hide the check box allowing the user to restore the connection at logon.</summary>
			/// <value><c>true</c> if hiding restore connection check box; otherwise, <c>false</c>.</value>
			[DefaultValue(false), Category("Appearance"), Description("Hide the check box allowing the user to restore the connection at logon.")]
			public bool HideRestoreConnectionCheckBox
			{
				get => dialogOptions.dwFlags.IsFlagSet(Vanara.PInvoke.Mpr.CONN_DLG.CONNDLG_HIDE_BOX);
				set => dialogOptions.dwFlags = dialogOptions.dwFlags.SetFlags(Vanara.PInvoke.Mpr.CONN_DLG.CONNDLG_HIDE_BOX, value);
			}

			/// <summary>Gets or sets a value indicating whether restore the connection at logon.</summary>
			/// <value><c>true</c> to restore connection at logon; otherwise, <c>false</c>.</value>
			[DefaultValue(false), Category("Behavior"), Description("Restore the connection at logon.")]
			public bool PersistConnectionAtLogon
			{
				get => dialogOptions.dwFlags.IsFlagSet(Vanara.PInvoke.Mpr.CONN_DLG.CONNDLG_PERSIST);
				set
				{
					dialogOptions.dwFlags = dialogOptions.dwFlags.SetFlags(Vanara.PInvoke.Mpr.CONN_DLG.CONNDLG_PERSIST, value);
					dialogOptions.dwFlags = dialogOptions.dwFlags.SetFlags(Vanara.PInvoke.Mpr.CONN_DLG.CONNDLG_NOT_PERSIST, !value);
				}
			}

			/// <summary>
			/// Gets or sets a value indicating whether to display a read-only path instead of allowing the user to type in a path. This is only
			/// valid if <see cref="RemoteNetworkName"/> is not <see langword="null"/>.
			/// </summary>
			/// <value><c>true</c> to display a read only path; otherwise, <c>false</c>.</value>
			[DefaultValue(false), Category("Appearance"), Description("Display a read-only path instead of allowing the user to type in a path.")]
			public bool ReadOnlyPath { get; set; }

			/// <summary>Gets or sets the name of the remote network.</summary>
			/// <value>The name of the remote network.</value>
			[DefaultValue(null), Category("Behavior"), Description("The value displayed in the path field.")]
			public string RemoteNetworkName { get => netRes.lpRemoteName; set => netRes.lpRemoteName = value; }

			/// <summary>Gets or sets a value indicating whether to enter the most recently used paths into the combination box.</summary>
			/// <value><c>true</c> to use MRU path; otherwise, <c>false</c>.</value>
			/// <exception cref="InvalidOperationException">UseMostRecentPath</exception>
			[DefaultValue(false), Category("Behavior"), Description("Enter the most recently used paths into the combination box.")]
			public bool UseMostRecentPath
			{
				get => dialogOptions.dwFlags.IsFlagSet(Vanara.PInvoke.Mpr.CONN_DLG.CONNDLG_USE_MRU);
				set
				{
					if (value && !string.IsNullOrEmpty(RemoteNetworkName))
						throw new InvalidOperationException($"{nameof(UseMostRecentPath)} cannot be set to true if {nameof(RemoteNetworkName)} has a value.");

					dialogOptions.dwFlags = dialogOptions.dwFlags.SetFlags(Vanara.PInvoke.Mpr.CONN_DLG.CONNDLG_USE_MRU, value);
				}
			}

			/// <inheritdoc/>
			public override void Reset()
			{
				dialogOptions.dwDevNum = -1;
				dialogOptions.dwFlags = 0;
				dialogOptions.lpConnRes = IntPtr.Zero;
				ReadOnlyPath = false;
			}

			/// <inheritdoc/>
			protected override bool RunDialog(IntPtr hwndOwner)
			{
				using var lpNetResource = Vanara.InteropServices.SafeCoTaskMemHandle.CreateFromStructure(netRes);

				dialogOptions.hwndOwner = hwndOwner;
				dialogOptions.lpConnRes = lpNetResource.DangerousGetHandle();

				if (ReadOnlyPath && !string.IsNullOrEmpty(netRes.lpRemoteName))
					dialogOptions.dwFlags |= Vanara.PInvoke.Mpr.CONN_DLG.CONNDLG_RO_PATH;

				var result = Vanara.PInvoke.Mpr.WNetConnectionDialog1(dialogOptions);

				dialogOptions.lpConnRes = IntPtr.Zero;

				if (result == unchecked((uint)-1))
					return false;

				result.ThrowIfFailed();

				return true;
			}
		}
	}
}

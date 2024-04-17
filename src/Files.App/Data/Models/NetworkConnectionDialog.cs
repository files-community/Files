// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;
using System.Windows.Forms;
using Vanara.Extensions;
using Vanara.InteropServices;
using static Vanara.PInvoke.Mpr;

namespace Files.App.Data.Models
{
	/// <summary>
	/// A dialog box that allows the user to browse and connect to network resources.
	/// </summary>
	/// <remarks>
	/// Forked from Vanara.
	/// </remarks>
	public sealed class NetworkConnectionDialog : CommonDialog
	{
		private readonly NETRESOURCE netRes = new();
		private CONNECTDLGSTRUCT dialogOptions;

		/// <summary>Initializes a new instance of the <see cref="NetworkConnectionDialog"/> class.</summary>
		public NetworkConnectionDialog()
		{
			dialogOptions.cbStructure = (uint)Marshal.SizeOf(typeof(CONNECTDLGSTRUCT));
			netRes.dwType = NETRESOURCEType.RESOURCETYPE_DISK;
		}

		/// <summary>Gets the connected device number. This value is only valid after successfully running the dialog.</summary>
		/// <value>The connected device number. The value is 1 for A:, 2 for B:, 3 for C:, and so on. If the user made a deviceless connection, the value is –1.</value>
		[Browsable(false)]
		public int ConnectedDeviceNumber => dialogOptions.dwDevNum;

		/// <summary>Gets or sets a value indicating whether to hide the check box allowing the user to restore the connection at logon.</summary>
		/// <value><c>true</c> if hiding restore connection check box; otherwise, <c>false</c>.</value>
		[DefaultValue(false), Category("Appearance"), Description("Hide the check box allowing the user to restore the connection at logon.")]
		public bool HideRestoreConnectionCheckBox
		{
			get => dialogOptions.dwFlags.IsFlagSet(CONN_DLG.CONNDLG_HIDE_BOX);
			set => dialogOptions.dwFlags = dialogOptions.dwFlags.SetFlags(CONN_DLG.CONNDLG_HIDE_BOX, value);
		}

		/// <summary>Gets or sets a value indicating whether restore the connection at logon.</summary>
		/// <value><c>true</c> to restore connection at logon; otherwise, <c>false</c>.</value>
		[DefaultValue(false), Category("Behavior"), Description("Restore the connection at logon.")]
		public bool PersistConnectionAtLogon
		{
			get => dialogOptions.dwFlags.IsFlagSet(CONN_DLG.CONNDLG_PERSIST);
			set
			{
				dialogOptions.dwFlags = dialogOptions.dwFlags.SetFlags(CONN_DLG.CONNDLG_PERSIST, value);
				dialogOptions.dwFlags = dialogOptions.dwFlags.SetFlags(CONN_DLG.CONNDLG_NOT_PERSIST, !value);
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
			get => dialogOptions.dwFlags.IsFlagSet(CONN_DLG.CONNDLG_USE_MRU);
			set
			{
				if (value && !string.IsNullOrEmpty(RemoteNetworkName))
					throw new InvalidOperationException($"{nameof(UseMostRecentPath)} cannot be set to true if {nameof(RemoteNetworkName)} has a value.");

				dialogOptions.dwFlags = dialogOptions.dwFlags.SetFlags(CONN_DLG.CONNDLG_USE_MRU, value);
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
			using var lpNetResource = SafeCoTaskMemHandle.CreateFromStructure(netRes);

			dialogOptions.hwndOwner = hwndOwner;
			dialogOptions.lpConnRes = lpNetResource.DangerousGetHandle();

			if (ReadOnlyPath && !string.IsNullOrEmpty(netRes.lpRemoteName))
				dialogOptions.dwFlags |= CONN_DLG.CONNDLG_RO_PATH;

			var result = WNetConnectionDialog1(dialogOptions);

			dialogOptions.lpConnRes = IntPtr.Zero;

			if (result == unchecked((uint)-1))
				return false;

			result.ThrowIfFailed();

			return true;
		}
	}
}

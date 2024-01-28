// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;
using System.Windows.Forms;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;

namespace Files.App.Data.Models
{
	/// <summary>
	/// Represents a <see cref="CommonDialog"/> that allows the user to browse and connect to network resources.
	/// </summary>
	public class NetworkConnectionCommonDialog : CommonDialog
	{
		private readonly Mpr.NETRESOURCE netResource = new();

		private Mpr.CONNECTDLGSTRUCT connectDlgStruct;

		/// <summary>
		/// Initializes a new instance of the <see cref="NetworkConnectionCommonDialog"/> class.
		/// </summary>
		public NetworkConnectionCommonDialog()
		{
			connectDlgStruct.cbStructure = (uint)Marshal.SizeOf(typeof(Mpr.CONNECTDLGSTRUCT));
			netResource.dwType = Mpr.NETRESOURCEType.RESOURCETYPE_DISK;
		}

		/// <summary>
		/// Gets the connected device number. This value is only valid after successfully running the dialog.
		/// </summary>
		/// <value>
		/// The connected device number. The value is 1 for A:, 2 for B:, 3 for C:, and so on. If the user made a deviceless connection, the value is –1.
		/// </value>
		[Browsable(false)]
		public int ConnectedDeviceNumber
			=> connectDlgStruct.dwDevNum;

		/// <summary>
		/// Gets or sets a value indicating whether to hide the check box allowing the user to restore the connection at logon.
		/// </summary>
		/// <value>
		/// true if hiding restore connection check box; otherwise, false.
		/// </value>
		[DefaultValue(false), Category("Appearance"), Description("Hide the check box allowing the user to restore the connection at logon.")]
		public bool HideRestoreConnectionCheckBox
		{
			get => connectDlgStruct.dwFlags.IsFlagSet(Mpr.CONN_DLG.CONNDLG_HIDE_BOX);
			set => connectDlgStruct.dwFlags = connectDlgStruct.dwFlags.SetFlags(Mpr.CONN_DLG.CONNDLG_HIDE_BOX, value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether restore the connection at logon.
		/// </summary>
		/// <value>
		/// true to restore connection at logon; otherwise, false.
		/// </value>
		[DefaultValue(false), Category("Behavior"), Description("Restore the connection at logon.")]
		public bool PersistConnectionAtLogon
		{
			get => connectDlgStruct.dwFlags.IsFlagSet(Mpr.CONN_DLG.CONNDLG_PERSIST);
			set
			{
				connectDlgStruct.dwFlags = connectDlgStruct.dwFlags.SetFlags(Mpr.CONN_DLG.CONNDLG_PERSIST, value);
				connectDlgStruct.dwFlags = connectDlgStruct.dwFlags.SetFlags(Mpr.CONN_DLG.CONNDLG_NOT_PERSIST, !value);
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to display a read-only path instead of allowing the user to type in a path. This is only
		/// valid if <see cref="RemoteNetworkName"/> is not <see langword="null"/>.
		/// </summary>
		/// <value>
		/// true to display a read only path; otherwise, false.
		/// </value>
		[DefaultValue(false), Category("Appearance"), Description("Display a read-only path instead of allowing the user to type in a path.")]
		public bool ReadOnlyPath { get; set; }

		/// <summary>
		/// Gets or sets the name of the remote network.
		/// </summary>
		/// <value>
		/// The name of the remote network.
		/// </value>
		[DefaultValue(null), Category("Behavior"), Description("The value displayed in the path field.")]
		public string RemoteNetworkName { get => netResource.lpRemoteName; set => netResource.lpRemoteName = value; }

		/// <summary>
		/// Gets or sets a value indicating whether to enter the most recently used paths into the combination box.
		/// </summary>
		/// <value>
		/// true to use MRU path; otherwise, false.
		/// </value>
		/// <exception cref="InvalidOperationException">UseMostRecentPath</exception>
		[DefaultValue(false), Category("Behavior"), Description("Enter the most recently used paths into the combination box.")]
		public bool UseMostRecentPath
		{
			get => connectDlgStruct.dwFlags.IsFlagSet(Mpr.CONN_DLG.CONNDLG_USE_MRU);
			set
			{
				if (value && !string.IsNullOrEmpty(RemoteNetworkName))
					throw new InvalidOperationException($"{nameof(UseMostRecentPath)} cannot be set to true if {nameof(RemoteNetworkName)} has a value.");

				connectDlgStruct.dwFlags = connectDlgStruct.dwFlags.SetFlags(Mpr.CONN_DLG.CONNDLG_USE_MRU, value);
			}
		}

		/// <inheritdoc/>
		public override void Reset()
		{
			connectDlgStruct.dwDevNum = -1;
			connectDlgStruct.dwFlags = 0;
			connectDlgStruct.lpConnRes = IntPtr.Zero;
			ReadOnlyPath = false;
		}

		/// <inheritdoc/>
		protected override bool RunDialog(IntPtr hwndOwner)
		{
			using var lpNetworkResource = SafeCoTaskMemHandle.CreateFromStructure(netResource);

			connectDlgStruct.hwndOwner = hwndOwner;
			connectDlgStruct.lpConnRes = lpNetworkResource.DangerousGetHandle();

			if (ReadOnlyPath && !string.IsNullOrEmpty(netResource.lpRemoteName))
				connectDlgStruct.dwFlags |= Mpr.CONN_DLG.CONNDLG_RO_PATH;

			var ret = Mpr.WNetConnectionDialog1(connectDlgStruct);

			connectDlgStruct.lpConnRes = IntPtr.Zero;

			if (ret == unchecked((uint)-1))
				return false;

			ret.ThrowIfFailed();

			return true;
		}
	}
}

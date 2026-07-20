// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace Files.App.Utils.Shell
{
	internal static class DetectionAndSharingHelper
	{
		public static unsafe NetworkAvailability GetNetworkAvailability()
		{
			IDetectionAndSharing dtsh = CreateDetectionAndSharing();
			var availability = NetworkAvailability.None;

			if (IsEnabled(dtsh, DTSH_TYPE.DTSH_NETWORK_DISCOVERY))
				availability |= NetworkAvailability.Discovery;

			if (IsEnabled(dtsh, DTSH_TYPE.DTSH_FILE_SHARING))
				availability |= NetworkAvailability.Sharing;

			return availability;
		}

		public static unsafe void OpenNetworkSharingSettings()
		{
			HRESULT hr = PInvoke.CoCreateInstance(CLSID.CLSID_OpenControlPanel, null, CLSCTX.CLSCTX_INPROC_SERVER, out IOpenControlPanel? controlPanel);
			ThrowIfFailed(hr, "Failed to create open control panel object.");

			fixed (char* name = "Microsoft.NetworkAndSharingCenter")
			fixed (char* page = "Advanced")
			{
				hr = controlPanel!.Open(name, page, null);
				ThrowIfFailed(hr, "Failed to open advanced sharing settings.");
			}
		}

		private static unsafe IDetectionAndSharing CreateDetectionAndSharing()
		{
			HRESULT hr = PInvoke.CoCreateInstance(CLSID.CLSID_DetectionAndSharing, null, CLSCTX.CLSCTX_INPROC_SERVER, out IDetectionAndSharing? dtsh);
			ThrowIfFailed(hr, "Failed to create detection and sharing object.");

			return dtsh!;
		}

		private static unsafe bool IsEnabled(IDetectionAndSharing dtsh, DTSH_TYPE type)
		{
			HRESULT hr = dtsh.GetStatus(type, out DTSH_STATE state, out _);
			ThrowIfFailed(hr, $"Failed to get {type} status.");

			return state is DTSH_STATE.DTSH_ON;
		}

		private static void ThrowIfFailed(HRESULT hr, string message)
		{
			if (hr.Failed)
				throw new COMException(message, hr.Value);
		}
	}
}

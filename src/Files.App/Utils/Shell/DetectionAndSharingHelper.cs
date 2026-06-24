// Copyright (c) Files Community
// Licensed under the MIT License.

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
			using ComPtr<IDetectionAndSharing> dtsh = CreateDetectionAndSharing();
			var availability = NetworkAvailability.None;

			if (IsEnabled(dtsh.Get(), DTSH_TYPE.DTSH_NETWORK_DISCOVERY))
				availability |= NetworkAvailability.Discovery;

			if (IsEnabled(dtsh.Get(), DTSH_TYPE.DTSH_FILE_SHARING))
				availability |= NetworkAvailability.Sharing;

			return availability;
		}

		public static unsafe void OpenNetworkSharingSettings()
		{
			using ComPtr<IOpenControlPanel> controlPanel = default;
			HRESULT hr = controlPanel.CoCreateInstance(CLSID.CLSID_OpenControlPanel, null, CLSCTX.CLSCTX_INPROC_SERVER);
			ThrowIfFailed(hr, "Failed to create open control panel object.");

			fixed (char* name = "Microsoft.NetworkAndSharingCenter")
			fixed (char* page = "Advanced")
			{
				hr = controlPanel.Get()->Open(name, page, null);
				ThrowIfFailed(hr, "Failed to open advanced sharing settings.");
			}
		}

		private static unsafe ComPtr<IDetectionAndSharing> CreateDetectionAndSharing()
		{
			ComPtr<IDetectionAndSharing> dtsh = default;
			HRESULT hr = dtsh.CoCreateInstance(CLSID.CLSID_DetectionAndSharing, null, CLSCTX.CLSCTX_INPROC_SERVER);
			ThrowIfFailed(hr, "Failed to create detection and sharing object.");

			return dtsh;
		}

		private static unsafe bool IsEnabled(IDetectionAndSharing* dtsh, DTSH_TYPE type)
		{
			DTSH_STATE state;
			DTSH_ACTION action;
			HRESULT hr = dtsh->GetStatus(type, &state, &action);
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

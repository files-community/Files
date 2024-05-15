// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;
using Vanara.PInvoke;

namespace Files.App.Services.PreviewPopupProviders
{
	public struct COPYDATASTRUCT
	{
		public IntPtr dwData;
		public int cbData;
		public IntPtr lpData;
	}

	public sealed class SeerProProvider : IPreviewPopupProvider
	{
		public static SeerProProvider Instance { get; } = new();

		private string? CurrentPath;

		public async Task TogglePreviewPopupAsync(string path)
		{
			HWND Window = User32.FindWindow("SeerWindowClass", null);
			COPYDATASTRUCT data = new COPYDATASTRUCT();
			data.dwData = 5000;
			data.cbData = (path.Length + 1) * 2;
			data.lpData = Marshal.StringToHGlobalUni(path);
			User32.SendMessage(Window, (uint)User32.WindowMessage.WM_COPYDATA, 0, ref data);

			CurrentPath = User32.IsWindowVisible(Window) ? path : null;
		}

		public async Task SwitchPreviewAsync(string path)
		{
			// Close preview window is track selection setting is disabled
			if (!IsTrackSelectionSettingEnabled && !string.IsNullOrEmpty(CurrentPath))
			{
				await TogglePreviewPopupAsync(CurrentPath);
				return;				
			}

			// Update the preview window if the path changed
			if (CurrentPath is not null && path != CurrentPath)
				await TogglePreviewPopupAsync(path);
		}

		public async Task<bool> DetectAvailability()
		{
			var handle = User32.FindWindow("SeerWindowClass", null).DangerousGetHandle();
			return handle != IntPtr.Zero && handle.ToInt64() != -1;
		}

		private bool? _IsTrackSelectionSettingEnabledCache;
		private bool IsTrackSelectionSettingEnabled
		{
			get
			{
				if (_IsTrackSelectionSettingEnabledCache is null)
					_IsTrackSelectionSettingEnabledCache = DetectTrackSelectionSetting().Result;

				return _IsTrackSelectionSettingEnabledCache.Value;
			}
		}

		private Task<bool> DetectTrackSelectionSetting()
		{
			bool trackSelectedFile = true;

			var keyName = @"HKEY_CURRENT_USER\Software\Corey\Seer";
			var value = Registry.GetValue(keyName, "tracking_file", null);

			if (bool.TryParse(value?.ToString(), out var result))
				return Task.FromResult(result);

			// List of possible paths for the Seer Pro settings file
			string[] paths =
			{
				Environment.ExpandEnvironmentVariables("%USERPROFILE%\\Documents\\Seer\\uwp.ini"),
				Environment.ExpandEnvironmentVariables("%USERPROFILE%\\appdata\\Local\\Packages\\CNABA5E861-AC2A-4523-B3C1.Seer-AWindowsQuickLookTo_p7t0z30wh4868\\LocalCache\\Local\\Corey\\Seer\\uwp.ini"),
				Environment.ExpandEnvironmentVariables("%USERPROFILE%\\appdata\\Local\\Corey\\Seer\\uwp.ini"),
				Environment.ExpandEnvironmentVariables("%USERPROFILE%\\Documents\\Seer\\config.ini")
			};

			// Find the first existing path
			foreach (var path in paths)
			{
				if (File.Exists(path))
				{
					// Read the settings file and look for the tracking_file setting
					string[] lines = File.ReadAllLines(path);

					foreach (var line in lines)
					{
						if (line.StartsWith("tracking_file", StringComparison.OrdinalIgnoreCase))
						{
							string[] keyValue = line.Split('=');
							if (keyValue.Length == 2 && bool.TryParse(keyValue[1].Trim(), out bool isTrackingFile))
							{
								trackSelectedFile = isTrackingFile;
								break;
							}
						}
					}

					break;
				}
			}

			return Task.FromResult(trackSelectedFile);
		}
	}
}

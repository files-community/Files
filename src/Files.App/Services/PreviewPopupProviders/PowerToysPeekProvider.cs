// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using Microsoft.Win32;

namespace Files.App.Services.PreviewPopupProviders
{
	public sealed class PowerToysPeekProvider : IPreviewPopupProvider
	{
		public static PowerToysPeekProvider Instance { get; } = new();

		private static string? _peekExecutablePath;

		public async Task TogglePreviewPopupAsync(string path)
		{
			await DoPreviewAsync(path);
		}

		public async Task SwitchPreviewAsync(string path)
		{
			// Not used
		}

		private async Task DoPreviewAsync(string path)
		{
			if (_peekExecutablePath != null)
			{
				try
				{
					var psi = new ProcessStartInfo
					{
						FileName = _peekExecutablePath,
						Arguments = $"\"{path}\"",
						UseShellExecute = true,
						CreateNoWindow = true,
						WindowStyle = ProcessWindowStyle.Hidden
					};

					Process.Start(psi);
				}
				catch
				{
					// Ignore
				}
			}
		}

		public async Task<bool> DetectAvailability()
		{
			var exeName = "PowerToys.Peek.UI.exe";
			if (FindPeekPathFromRegistry(exeName) is { } registryPath && File.Exists(registryPath))
			{
				_peekExecutablePath = registryPath;
				return true;
			}

			// Not found
			return false;
		}

		private static string? FindPeekPathFromRegistry(string exeName)
		{
			string[] uninstallRegistryPaths =
			[
				@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
				@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
			];

			foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
			{
				foreach (var uninstallPath in uninstallRegistryPaths)
				{
					try
					{
						using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
						using var uninstallKey = baseKey.OpenSubKey(uninstallPath);
						if (uninstallKey is null)
							continue;

						foreach (var subKeyName in uninstallKey.GetSubKeyNames())
						{
							using var appKey = uninstallKey.OpenSubKey(subKeyName);
							if (appKey is null)
								continue;

							var displayName = appKey.GetValue("DisplayName") as string;
							if (string.IsNullOrWhiteSpace(displayName)
								|| displayName.IndexOf("PowerToys", StringComparison.OrdinalIgnoreCase) < 0)
								continue;

							var installLocation = appKey.GetValue("InstallLocation") as string;
							if (!string.IsNullOrWhiteSpace(installLocation))
								return Path.Combine(installLocation, "WinUI3Apps", exeName);
						}
					}
					catch
					{
						// Ignore registry access issues and continue fallback resolution.
					}
				}
			}

			return null;
		}
	}
}

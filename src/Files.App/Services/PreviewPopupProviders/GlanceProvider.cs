// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace Files.App.Services.PreviewPopupProviders
{
	public sealed class GlanceProvider : IPreviewPopupProvider
	{
		public static GlanceProvider Instance { get; } = new();

		private static string? _cliExecutablePath;
		private string? _currentPath;

		public async Task TogglePreviewPopupAsync(string path)
		{
			if (!string.IsNullOrEmpty(_currentPath) &&
				string.Equals(_currentPath, path, StringComparison.OrdinalIgnoreCase))
			{
				await RunCliAsync("window", "close", "--timeout", "0");
				_currentPath = null;
				return;
			}

			if (await RunCliAsync("preview", path, "--timeout", "0"))
				_currentPath = path;
			else
				_currentPath = null;
		}

		public async Task SwitchPreviewAsync(string path)
		{
			if (_currentPath is null || string.Equals(path, _currentPath, StringComparison.OrdinalIgnoreCase))
				return;

			if (await RunCliAsync("window", "set", path, "--timeout", "0"))
				_currentPath = path;
			else
				_currentPath = null;
		}

		private static async Task<bool> RunCliAsync(params string[] arguments)
		{
			if (_cliExecutablePath is null)
				return false;

			try
			{
				var psi = new ProcessStartInfo
				{
					FileName = _cliExecutablePath,
					UseShellExecute = false,
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden
				};

				foreach (var argument in arguments)
					psi.ArgumentList.Add(argument);

				using var process = Process.Start(psi);
				if (process is null)
					return false;

				await process.WaitForExitAsync();
				return process.ExitCode == 0;
			}
			catch
			{
				return false;
			}
		}

		public async Task<bool> DetectAvailability()
		{
			var isRunning = Process.GetProcessesByName("Glance").Length > 0;
			if (!isRunning)
				return false;

			if (_cliExecutablePath is not null && File.Exists(_cliExecutablePath))
				return true;

			var exeName = "Glance.CLI.exe";
			if (FindGlancePathFromRegistry(exeName) is { } registryPath && File.Exists(registryPath))
			{
				_cliExecutablePath = registryPath;
				return true;
			}

			var defaultPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
				"Glance",
				exeName);

			if (File.Exists(defaultPath))
			{
				_cliExecutablePath = defaultPath;
				return true;
			}

			return false;
		}

		private static string? FindGlancePathFromRegistry(string exeName)
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
								|| !displayName.StartsWith("Glance", StringComparison.OrdinalIgnoreCase))
								continue;

							var installLocation = appKey.GetValue("InstallLocation") as string;
							if (!string.IsNullOrWhiteSpace(installLocation))
								return Path.Combine(installLocation, exeName);
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

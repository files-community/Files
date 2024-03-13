// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Win32;

namespace Files.App.Services
{
	/// <inheritdoc cref="IWindowsCompatibilityService"/>
	public class WindowsCompatibilityService : IWindowsCompatibilityService
	{
		private const string RegistrySubPath = "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers";

		/// <inheritdoc/>
		public WindowsCompatibilityOptions GetCompatibilityOptionsForPath(string filePath)
		{
			try
			{
				// Get the key
				using var compatKey = Registry.CurrentUser.OpenSubKey(RegistrySubPath);
				if (compatKey is null)
					return new();

				// Get the value for the specified path
				var stringOptions = (string?)compatKey.GetValue(filePath, null);

				return WindowsCompatibilityOptions.FromString(stringOptions);
			}
			catch (Exception)
			{
				return new();
			}
		}

		/// <inheritdoc/>
		public bool SetCompatibilityOptionsForPath(string filePath, WindowsCompatibilityOptions options)
		{
			var stringOptions = options.ToString();

			// Remove old one if new one is valid
			if (string.IsNullOrEmpty(stringOptions) || stringOptions == "~")
			{
				return Win32API.RunPowershellCommand(
					@$"Remove-ItemProperty -Path 'HKCU:\{RegistrySubPath}' -Name '{filePath}' | Out-Null",
					true);
			}

			// Set the new one
			return Win32API.RunPowershellCommand(
				@$"New-ItemProperty -Path 'HKCU:\{RegistrySubPath}' -Name '{filePath}' -Value '{options}' -PropertyType String -Force | Out-Null",
				true);
		}
	}
}

// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Win32;

namespace Files.App.Services
{
	/// <inheritdoc cref="IWindowsCompatibilityService"/>
	public sealed class WindowsCompatibilityService : IWindowsCompatibilityService
	{
		private readonly string _registrySubPath = "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers";

		/// <inheritdoc/>
		public WindowsCompatibilityOptions GetCompatibilityOptionsForPath(string filePath)
		{
			try
			{
				// Get the key
				using var compatKey = Registry.CurrentUser.OpenSubKey(_registrySubPath);
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
			var registryPath = Win32Helper.ToPowerShellStringLiteral($@"HKCU:\{_registrySubPath}");
			var propertyName = Win32Helper.ToPowerShellStringLiteral(filePath);

			// Remove old one if new one is valid
			if (string.IsNullOrEmpty(stringOptions) || stringOptions == "~")
			{
				return Win32Helper.RunPowershellCommand(
					$"Remove-ItemProperty -Path {registryPath} -Name {propertyName} | Out-Null",
					PowerShellExecutionOptions.Elevated | PowerShellExecutionOptions.Hidden);
			}

			// Set the new one
			return Win32Helper.RunPowershellCommand(
				$"New-ItemProperty -Path {registryPath} -Name {propertyName} -Value {Win32Helper.ToPowerShellStringLiteral(stringOptions)} -PropertyType String -Force | Out-Null",
				PowerShellExecutionOptions.Elevated | PowerShellExecutionOptions.Hidden);
		}
	}
}

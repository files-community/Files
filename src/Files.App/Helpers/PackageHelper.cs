// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.System;

namespace Files.App.Helpers
{
	public static class PackageHelper
	{
		private static readonly Uri dummyUri = new("mailto:dummy@dummy.com");

		/// <summary>
		/// Check if target <paramref name="packageName"/> is installed on this device.
		/// </summary>
		/// <param name="packageName">Package name in format: "949FFEAB.Email.cz_refxrrjvvv3cw"</param>
		/// <returns>True is app is installed on this device, false otherwise.</returns>
		public static async Task<bool> IsAppInstalledAsync(string packageName)
		{
			try
			{
				LaunchQuerySupportStatus result = await Launcher.QueryUriSupportAsync(dummyUri, LaunchQuerySupportType.Uri, packageName);

				var appInstalled = result switch
				{
					LaunchQuerySupportStatus.Available or LaunchQuerySupportStatus.NotSupported => true,
					//case LaunchQuerySupportStatus.AppNotInstalled:
					//case LaunchQuerySupportStatus.AppUnavailable:
					//case LaunchQuerySupportStatus.Unknown:
					_ => false,
				};

				Debug.WriteLine($"App {packageName}, query status: {result}, installed: {appInstalled}");

				return appInstalled;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error checking if app {packageName} is installed. Error: {ex}");

				return false;
			}
		}
	}
}

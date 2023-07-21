// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.System;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides static helper for package information of the app.
	/// </summary>
	public static class AppPackageHelper
	{
		private static readonly Uri dummyUri = new("mailto:dummy@dummy.com");

		/// <summary>
		/// Checks if target <paramref name="packageName"/> is installed on this device.
		/// </summary>
		/// <param name="packageName">Package name in format: "949FFEAB.Email.cz_refxrrjvvv3cw"</param>
		/// <returns>
		/// Returns true if app is installed on this device; otherwise, false.
		/// </returns>
		public static async Task<bool> IsAppInstalledAsync(string packageName)
		{
			try
			{
				bool appInstalled;
				LaunchQuerySupportStatus result = await Launcher.QueryUriSupportAsync(dummyUri, LaunchQuerySupportType.Uri, packageName);
				switch (result)
				{
					case LaunchQuerySupportStatus.Available:
					case LaunchQuerySupportStatus.NotSupported:
						appInstalled = true;
						break;
					//case LaunchQuerySupportStatus.AppNotInstalled:
					//case LaunchQuerySupportStatus.AppUnavailable:
					//case LaunchQuerySupportStatus.Unknown:
					default:
						appInstalled = false;
						break;
				}

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

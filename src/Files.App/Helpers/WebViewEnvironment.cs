// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using System.IO;
using Windows.Storage;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides the WebView2 environment shared by the web views of the app.
	/// </summary>
	internal static class WebViewEnvironment
	{
		private const string UserDataFolderName = "WebView2";

		// The folder WebView2 creates on its own when no environment is specified
		private const string DefaultUserDataFolderName = "EBWebView";

		private static Task<CoreWebView2Environment>? _environment;

		/// <summary>
		/// Gets the environment, which keeps the browser profile in the temporary folder of the app and doesn't download optional browser components.
		/// </summary>
		public static Task<CoreWebView2Environment> GetAsync()
		{
			var environment = _environment;
			if (environment is null or { IsFaulted: true })
				_environment = environment = CreateAsync();

			return environment;
		}

		private static async Task<CoreWebView2Environment> CreateAsync()
		{
			var defaultUserDataFolder = Path.Combine(ApplicationData.Current.LocalFolder.Path, DefaultUserDataFolderName);
			_ = Task.Run(() =>
			{
				if (Directory.Exists(defaultUserDataFolder))
					SafetyExtensions.IgnoreExceptions(() => Directory.Delete(defaultUserDataFolder, true), App.Logger);
			});

			var userDataFolder = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, UserDataFolderName);
			var options = new CoreWebView2EnvironmentOptions() { AdditionalBrowserArguments = "--disable-component-update" };

			return await CoreWebView2Environment.CreateWithOptionsAsync(null, userDataFolder, options);
		}
	}
}

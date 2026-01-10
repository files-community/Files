// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Actions;
using Files.App.Data.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using System.Text.RegularExpressions;
using Windows.System;

namespace Files.App.Views
{
	public sealed partial class ReleaseNotesPage : Page, IDisposable
	{
		// Dependency injections
		public ReleaseNotesViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<ReleaseNotesViewModel>();

		private FrameworkElement RootAppElement
			=> (FrameworkElement)MainWindow.Instance.Content;

		private IShellPage AppInstance { get; set; } = null!;

		public ReleaseNotesPage()
		{
			InitializeComponent();
		}

		protected override async void OnNavigatedTo(NavigationEventArgs e)
		{
			if (e.Parameter is not NavigationArguments parameters)
				return;

			AppInstance = parameters.AssociatedTabInstance!;

			AppInstance.InstanceViewModel.IsPageTypeNotHome = true;
			AppInstance.InstanceViewModel.IsPageTypeSearchResults = false;
			AppInstance.InstanceViewModel.IsPageTypeMtpDevice = false;
			AppInstance.InstanceViewModel.IsPageTypeRecycleBin = false;
			AppInstance.InstanceViewModel.IsPageTypeCloudDrive = false;
			AppInstance.InstanceViewModel.IsPageTypeFtp = false;
			AppInstance.InstanceViewModel.IsPageTypeZipFolder = false;
			AppInstance.InstanceViewModel.IsPageTypeLibrary = false;
			AppInstance.InstanceViewModel.GitRepositoryPath = null;
			AppInstance.InstanceViewModel.IsGitRepository = false;
			AppInstance.InstanceViewModel.IsPageTypeReleaseNotes = true;
			AppInstance.ToolbarViewModel.CanRefresh = false;
			AppInstance.ToolbarViewModel.CanGoBack = AppInstance.CanNavigateBackward;
			AppInstance.ToolbarViewModel.CanGoForward = AppInstance.CanNavigateForward;
			AppInstance.ToolbarViewModel.CanNavigateToParent = false;

			// Set path of working directory empty
			await AppInstance.ShellViewModel.SetWorkingDirectoryAsync("ReleaseNotes");
			AppInstance.ShellViewModel.CheckForBackgroundImage();

			AppInstance.SlimContentPage?.StatusBarViewModel.UpdateGitInfo(false, string.Empty, null);

			AppInstance.ToolbarViewModel.PathComponents.Clear();

			string componentLabel =
				parameters?.NavPathParam == "ReleaseNotes"
					? Strings.ReleaseNotes.GetLocalizedResource()
					: parameters?.NavPathParam
				?? string.Empty;

			string tag = parameters?.NavPathParam ?? string.Empty;

			var item = new PathBoxItem()
			{
				Title = componentLabel,
				Path = tag,
				ChevronToolTip = string.Format(Strings.BreadcrumbBarChevronButtonToolTip.GetLocalizedResource(), componentLabel),
			};

			AppInstance.ToolbarViewModel.PathComponents.Add(item);

			base.OnNavigatedTo(e);
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			Dispose();
		}

		private async void BlogPostWebView_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
		{
			try
			{
				sender.CoreWebView2.Profile.PreferredColorScheme = (CoreWebView2PreferredColorScheme)RootAppElement.RequestedTheme;
				sender.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
				sender.CoreWebView2.Settings.AreDevToolsEnabled = false;
				sender.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
				sender.CoreWebView2.Settings.IsSwipeNavigationEnabled = false;

				// Hotkey values for CloseSelectedTabAction
				var action = new CloseSelectedTabAction();
				var hotKey1 = NormaliseKey(action.HotKey.Key.ToString());
				var hotKey1Modifier = HotKey.JavaScriptModifiers.GetValueRefOrNullRef(action.HotKey.Modifier);
				var hotKey2 = NormaliseKey(action.SecondHotKey.Key.ToString());
				var hotKey2Modifier = HotKey.JavaScriptModifiers.GetValueRefOrNullRef(action.SecondHotKey.Modifier);

				// Injected script for both click and keydown events
				var script = $$"""
					document.addEventListener('click', function(event) {
						var target = event.target;

						while (target && target.tagName !== 'A') {
							target = target.parentElement;
						}

						if (target && target.href) {
							event.preventDefault();

							window.chrome.webview.postMessage({
								type: 'link-click',
								key: target.href
							});
						}
					});

					window.addEventListener('keydown', function(event) {
						const hotkey = event.{{hotKey1Modifier}} && event.key === '{{hotKey1}}';
						const secondHotkey = event.{{hotKey2Modifier}} && event.key === '{{hotKey2}}';

						if (hotkey || secondHotkey) {
							window.chrome.webview.postMessage({
								type: 'shortcut',
								key: '{{action.HotKey.RawLabel}}'
							});
						}
					});
				""";

				await sender.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
				sender.WebMessageReceived += WebView_OpenLinkInWebBrowser;
				sender.WebMessageReceived += WebView_HandleShortcut;
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
			}
		}

		// Handle keyboard shortcuts (Files.App.Actions) from WebView
		private async void WebView_HandleShortcut(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
		{
			try
			{
				var json = args.WebMessageAsJson;
				var message = JsonSerializer.Deserialize<WebMessage>(json);

				if (message?.Type == "shortcut")
				{
					await new CloseSelectedTabAction().ExecuteAsync();
				}
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
			}
		}

		// Handle link clicks to open in external web browser
		private async void WebView_OpenLinkInWebBrowser(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
		{
			try
			{
				var json = args.WebMessageAsJson;
				var message = JsonSerializer.Deserialize<WebMessage>(json);

				if (message?.Type == "link-click")
				{
					// Open link in web browser
					if (Uri.TryCreate(message.Key, UriKind.Absolute, out Uri? uri))
						await Launcher.LaunchUriAsync(uri);

					// Navigate back to blog post
					if (sender.CoreWebView2.CanGoBack)
						sender.CoreWebView2.GoBack();
				}
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
			}
		}

		private static string NormaliseKey(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
				return string.Empty;

			// F1–F24
			if (Regex.IsMatch(key, @"^F\d{1,2}$", RegexOptions.IgnoreCase))
				return key.ToUpperInvariant();

			return key.ToLowerInvariant();
		}

		public void Dispose()
		{
			BlogPostWebView.Close();
		}
	}
}

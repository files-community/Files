// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using Windows.System;

namespace Files.App.Views
{
	public sealed partial class ReleaseNotesPage : Page, IDisposable
	{
		// Dependency injections
		public ReleaseNotesViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<ReleaseNotesViewModel>();
		private readonly ICommandManager Commands = Ioc.Default.GetRequiredService<ICommandManager>();


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

			// Update Info Pane to avoid showing items from the previous directory
			AppInstance.SlimContentPage?.InfoPaneViewModel.UpdateSelectedItemPreviewAsync();

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
				if (sender.CoreWebView2 is null)
					return;

				sender.CoreWebView2.Profile.PreferredColorScheme = (CoreWebView2PreferredColorScheme)RootAppElement.RequestedTheme;
				sender.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
				sender.CoreWebView2.Settings.AreDevToolsEnabled = false;
				sender.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
				sender.CoreWebView2.Settings.IsSwipeNavigationEnabled = false;

				var script = @"
					document.addEventListener('click', function(event) {
						var target = event.target;
						while (target && target.tagName !== 'A') {
							target = target.parentElement;
						}
						if (target && target.href) {
							event.preventDefault();
							window.chrome.webview.postMessage(target.href);
						}
					});
					document.addEventListener('keydown', function(event) {
						if (event.repeat) return;
						if (event.key === 'Shift' || event.key === 'Control' || event.key === 'Alt' || event.key === 'Meta') return;
						window.chrome.webview.postMessage(
							'hotkey:' + event.keyCode + ':' +
							(event.ctrlKey ? 1 : 0) + ':' +
							(event.altKey ? 1 : 0) + ':' +
							(event.shiftKey ? 1 : 0) + ':' +
							(event.metaKey ? 1 : 0));
					});
				";

				await sender.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
				sender.WebMessageReceived += WebView_WebMessageReceived;
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
			}
		}

		private async void WebView_WebMessageReceived(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
		{
			try
			{
				var coreWebView = sender.CoreWebView2;
				if (coreWebView is null)
					return;

				var message = args.TryGetWebMessageAsString();

				if (message is not null && message.StartsWith("hotkey:", StringComparison.Ordinal))
				{
					await TryExecuteForwardedHotKeyAsync(message);
					return;
				}

				// Open link in web browser
				if (Uri.TryCreate(message, UriKind.Absolute, out Uri? uri))
					await Launcher.LaunchUriAsync(uri);

				// Navigate back to blog post
				if (coreWebView.CanGoBack)
					coreWebView.GoBack();
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
			}
		}

		private async Task TryExecuteForwardedHotKeyAsync(string message)
		{
			// Format: "hotkey:<keyCode>:<ctrl>:<alt>:<shift>:<meta>" produced by the keydown listener injected into the WebView2 content.
			var parts = message.Split(':');
			if (parts.Length < 6 || !int.TryParse(parts[1], out var keyCode))
				return;

			var modifiers = KeyModifiers.None;
			if (parts[2] == "1") modifiers |= KeyModifiers.Ctrl;
			if (parts[3] == "1") modifiers |= KeyModifiers.Alt;
			if (parts[4] == "1") modifiers |= KeyModifiers.Shift;
			if (parts[5] == "1") modifiers |= KeyModifiers.Win;

			var command = Commands[new HotKey((Keys)keyCode, modifiers)];
			if (command.Code is not CommandCodes.None && command.IsExecutable)
				await command.ExecuteAsync();
		}

		public void Dispose()
		{
			BlogPostWebView.WebMessageReceived -= WebView_WebMessageReceived;
			BlogPostWebView.Close();
		}
	}
}

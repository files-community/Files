// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Windows.System;
using Microsoft.UI.Xaml.Navigation;

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
			BlogPostWebView.CoreWebView2.Profile.PreferredColorScheme = (CoreWebView2PreferredColorScheme)RootAppElement.RequestedTheme;
			BlogPostWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
			BlogPostWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
			BlogPostWebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
			BlogPostWebView.CoreWebView2.Settings.IsSwipeNavigationEnabled = false;

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
			";

			await sender.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
			sender.WebMessageReceived += WebView_WebMessageReceived;
		}

		private async void WebView_WebMessageReceived(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
		{
			// Open link in web browser
			if (Uri.TryCreate(args.TryGetWebMessageAsString(), UriKind.Absolute, out Uri? uri))
				await Launcher.LaunchUriAsync(uri);

			// Navigate back to blog post
			if (sender.CoreWebView2.CanGoBack)
				sender.CoreWebView2.GoBack();
		}

		public void Dispose()
		{
			BlogPostWebView.Close();
		}
	}
}

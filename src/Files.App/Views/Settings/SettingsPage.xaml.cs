// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Views.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Views
{
	public sealed partial class SettingsPage : Page
	{
		// Dependency injections
		public SettingsViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<SettingsViewModel>();

		private FrameworkElement RootAppElement
			=> (FrameworkElement)MainWindow.Instance.Content;

		private IShellPage AppInstance { get; set; } = null!;

		public SettingsPage()
		{
			InitializeComponent();

			// Manually invoke the UpdateVisualStates event to ensure the visual state is set correctly when the page is first loaded
			UpdateVisualStates(this.Width);
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
			AppInstance.InstanceViewModel.IsPageTypeReleaseNotes = false;
			AppInstance.InstanceViewModel.IsPageTypeSettings = true;
			AppInstance.ToolbarViewModel.CanRefresh = false;
			AppInstance.ToolbarViewModel.CanGoBack = AppInstance.CanNavigateBackward;
			AppInstance.ToolbarViewModel.CanGoForward = AppInstance.CanNavigateForward;
			AppInstance.ToolbarViewModel.CanNavigateToParent = false;

			// Set path of working directory empty
			await AppInstance.ShellViewModel.SetWorkingDirectoryAsync("Settings");
			AppInstance.ShellViewModel.CheckForBackgroundImage();

			AppInstance.SlimContentPage?.StatusBarViewModel.UpdateGitInfo(false, string.Empty, null);

			AppInstance.ToolbarViewModel.PathComponents.Clear();

			string componentLabel =
				parameters?.NavPathParam == "Settings"
					? Strings.Settings.GetLocalizedResource()
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

		public void NavigateTo(SettingsNavigationParams navParams)
		{
			var defaultTag = SettingsPageKind.AppearancePage.ToString();
			var oldSelection = MainSettingsNavigationView.MenuItems.FirstOrDefault(item => ((NavigationViewItem)item).IsSelected) as NavigationViewItem;
			var targetSection = MainSettingsNavigationView.MenuItems.FirstOrDefault(
				item => Enum.Parse<SettingsPageKind>(((NavigationViewItem)item).Tag.ToString() ?? defaultTag) == navParams.PageKind
			);
			if (oldSelection is not null)
				oldSelection.IsSelected = false;
			
			MainSettingsNavigationView.SelectedItem = targetSection;
		}

		private void MainSettingsNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
		{
			SettingsContentScrollViewer.ChangeView(null, 0, null, true);
			var selectedItem = (NavigationViewItem)args.SelectedItem;

			_ = Enum.Parse<SettingsPageKind>(selectedItem.Tag.ToString()) switch
			{
				SettingsPageKind.GeneralPage => SettingsContentFrame.Navigate(typeof(GeneralPage), null, new SuppressNavigationTransitionInfo()),
				SettingsPageKind.AppearancePage => SettingsContentFrame.Navigate(typeof(AppearancePage), null, new SuppressNavigationTransitionInfo()),
				SettingsPageKind.LayoutPage => SettingsContentFrame.Navigate(typeof(LayoutPage), null, new SuppressNavigationTransitionInfo()),
				SettingsPageKind.FoldersPage => SettingsContentFrame.Navigate(typeof(FoldersPage), null, new SuppressNavigationTransitionInfo()),
				SettingsPageKind.ActionsPage => SettingsContentFrame.Navigate(typeof(ActionsPage), null, new SuppressNavigationTransitionInfo()),
				SettingsPageKind.TagsPage => SettingsContentFrame.Navigate(typeof(TagsPage), null, new SuppressNavigationTransitionInfo()),
				SettingsPageKind.DevToolsPage => SettingsContentFrame.Navigate(typeof(DevToolsPage), null, new SuppressNavigationTransitionInfo()),
				SettingsPageKind.AdvancedPage => SettingsContentFrame.Navigate(typeof(AdvancedPage), null, new SuppressNavigationTransitionInfo()),
				SettingsPageKind.AboutPage => SettingsContentFrame.Navigate(typeof(AboutPage), null, new SuppressNavigationTransitionInfo()),
				_ => SettingsContentFrame.Navigate(typeof(AppearancePage), null, new SuppressNavigationTransitionInfo())
			};

			PageHeaderTextBlock.Text = selectedItem.Content.ToString();
		}

		private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			UpdateVisualStates(e.NewSize.Width);
		}

		private void UpdateVisualStates(double pageWidth)
		{
			// Define your width threshold (e.g., 700 pixels)
			if (pageWidth < 700)
				VisualStateManager.GoToState(this, "Narrow", true);
			else
				VisualStateManager.GoToState(this, "Wide", true);
		}
	}
}

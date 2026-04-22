// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Controls;
using Files.App.ViewModels.Settings;
using Files.App.Views.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Views
{
	public sealed partial class SettingsPage : Page
	{
		private readonly ICommandManager Commands = Ioc.Default.GetRequiredService<ICommandManager>();

		public SettingsPageViewModel ViewModel { get; } = new();

		private IShellPage AppInstance { get; set; } = null!;

		public SettingsPage()
		{
			InitializeComponent();
		}

		private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
		{
			UpdateSidebarVisualState(ActualWidth);
		}

		protected override async void OnNavigatedTo(NavigationEventArgs e)
		{
			if (e.Parameter is not NavigationArguments navArgs)
				return;

			AppInstance = navArgs.AssociatedTabInstance!;

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

			await AppInstance.ShellViewModel.SetWorkingDirectoryAsync("Settings");
			AppInstance.ShellViewModel.CheckForBackgroundImage();

			AppInstance.SlimContentPage?.StatusBarViewModel.UpdateGitInfo(false, string.Empty, null);
			AppInstance.SlimContentPage?.InfoPaneViewModel.UpdateSelectedItemPreviewAsync();

			AppInstance.ToolbarViewModel.PathComponents.Clear();

			var componentLabel =
				navArgs.NavPathParam == "Settings"
					? Strings.Settings.GetLocalizedResource()
					: navArgs.NavPathParam
				?? string.Empty;

			var tag = navArgs.NavPathParam ?? string.Empty;

			AppInstance.ToolbarViewModel.PathComponents.Add(new PathBoxItem()
			{
				Title = componentLabel,
				Path = tag,
				ChevronToolTip = string.Format(Strings.BreadcrumbBarChevronButtonToolTip.GetLocalizedResource(), componentLabel),
			});

			var selectedTag = navArgs?.SelectItems?.FirstOrDefault();
			var pageKind = Enum.TryParse<SettingsPageKind>(selectedTag, out var parsedPageKind)
				? parsedPageKind
				: SettingsPageKind.GeneralPage;

			NavigateTo(new SettingsNavigationParams() { PageKind = pageKind });

			base.OnNavigatedTo(e);
		}

		public void NavigateTo(SettingsNavigationParams navParams)
		{
			var item = ViewModel.NavigationItems.FirstOrDefault(x => x.PageKind == navParams.PageKind);
			if (item is null)
				return;

			SettingsSidebar.SelectedItem = item;
			ViewModel.SetSelectedPage(navParams.PageKind);
			NavigateToPage(navParams.PageKind);
		}

		private void SettingsSidebar_ItemInvoked(object sender, ItemInvokedEventArgs e)
		{
			if (sender is not SidebarItem { Item: SettingsNavigationItem navItem })
				return;

			if (ViewModel.SelectedPage == navItem.PageKind)
				return;

			ViewModel.SetSelectedPage(navItem.PageKind);
			SettingsContentScrollViewer.ChangeView(null, 0, null, true);
			NavigateToPage(navItem.PageKind);
		}

		private void NavigateToPage(SettingsPageKind selectedPage)
		{
			var pageType = selectedPage switch
			{
				SettingsPageKind.GeneralPage => typeof(GeneralPage),
				SettingsPageKind.AppearancePage => typeof(AppearancePage),
				SettingsPageKind.LayoutPage => typeof(LayoutPage),
				SettingsPageKind.FoldersPage => typeof(FoldersPage),
				SettingsPageKind.ActionsPage => typeof(ActionsPage),
				SettingsPageKind.TagsPage => typeof(TagsPage),
				SettingsPageKind.DevToolsPage => typeof(DevToolsPage),
				SettingsPageKind.AdvancedPage => typeof(AdvancedPage),
				SettingsPageKind.AboutPage => typeof(AboutPage),
				_ => typeof(GeneralPage),
			};

			SettingsContentFrame.Navigate(pageType, null, new SuppressNavigationTransitionInfo());
		}

		private void Grid_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			if (sender is FrameworkElement element)
				SettingsPageContextMenu.ShowAt(element, e.GetPosition(element));

			e.Handled = true;
		}

		private void SettingsPage_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			UpdateSidebarVisualState(e.NewSize.Width);
		}

		private void UpdateSidebarVisualState(double pageWidth)
		{
			VisualStateManager.GoToState(this,
				pageWidth >= 600 ? "ExpandedSidebarState" : "CompactSidebarState",
				true);
		}

	}
}
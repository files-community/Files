// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Views.Settings;
using Files.App.ViewModels.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Dialogs
{
	public sealed partial class MainSettingsPage : Page
	{
		private IShellPage AppInstance { get; set; } = null!;

		public MainSettingsPage()
		{
			InitializeComponent();
		}

		protected override async void OnNavigatedTo(NavigationEventArgs e)
		{
			if (e.Parameter is not NavigationArguments parameters)
				return;

			AppInstance = parameters.AssociatedTabInstance!;

			AppInstance.InstanceViewModel.IsPageTypeNotHome = true;
			AppInstance.InstanceViewModel.IsPageTypeSettings = true;
			AppInstance.InstanceViewModel.IsPageTypeSearchResults = false;
			AppInstance.InstanceViewModel.IsPageTypeMtpDevice = false;
			AppInstance.InstanceViewModel.IsPageTypeRecycleBin = false;
			AppInstance.InstanceViewModel.IsPageTypeCloudDrive = false;
			AppInstance.InstanceViewModel.IsPageTypeFtp = false;
			AppInstance.InstanceViewModel.IsPageTypeZipFolder = false;
			AppInstance.InstanceViewModel.IsPageTypeLibrary = false;
			AppInstance.InstanceViewModel.GitRepositoryPath = null;
			AppInstance.ToolbarViewModel.CanRefresh = true;
			AppInstance.ToolbarViewModel.CanGoBack = AppInstance.CanNavigateBackward;
			AppInstance.ToolbarViewModel.CanGoForward = AppInstance.CanNavigateForward;
			AppInstance.ToolbarViewModel.CanNavigateToParent = false;

			AppInstance.ToolbarViewModel.CanRefresh = false;

			// Set path of working directory empty
			await AppInstance.FilesystemViewModel.SetWorkingDirectoryAsync("Settings");

			AppInstance.SlimContentPage?.DirectoryPropertiesViewModel.UpdateGitInfo(false, string.Empty, null);

			AppInstance.ToolbarViewModel.PathComponents.Clear();

			string componentLabel =
				parameters?.NavPathParam == "Settings"
					? "Settings".GetLocalizedResource()
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

		private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
		{
			UpdateDialogLayout();
		}

		private void UpdateDialogLayout()
		{
			ContainerGrid.Height = MainWindow.Instance.Bounds.Height <= 760 ? MainWindow.Instance.Bounds.Height - 70 : 690;
			ContainerGrid.Width = MainWindow.Instance.Bounds.Width <= 1100 ? MainWindow.Instance.Bounds.Width : 1100;
			MainSettingsNavigationView.PaneDisplayMode = MainWindow.Instance.Bounds.Width < 700 ? NavigationViewPaneDisplayMode.LeftCompact : NavigationViewPaneDisplayMode.Left;
		}

		private void MainSettingsNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
		{
			var selectedItem = (NavigationViewItem)args.SelectedItem;
			int selectedItemTag = Convert.ToInt32(selectedItem.Tag);

			_ = selectedItemTag switch
			{
				0 => SettingsContentFrame.Navigate(typeof(GeneralPage)),
				1 => SettingsContentFrame.Navigate(typeof(AppearancePage)),
				2 => SettingsContentFrame.Navigate(typeof(LayoutPage)),
				3 => SettingsContentFrame.Navigate(typeof(FoldersPage)),
				4 => SettingsContentFrame.Navigate(typeof(TagsPage)),
				5 => SettingsContentFrame.Navigate(typeof(GitPage)),
				6 => SettingsContentFrame.Navigate(typeof(AdvancedPage)),
				7 => SettingsContentFrame.Navigate(typeof(AboutPage)),
				_ => SettingsContentFrame.Navigate(typeof(AppearancePage))
			};
		}

		private void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
		{
			MainWindow.Instance.SizeChanged -= Current_SizeChanged;
		}

		private void CloseSettingsDialogButton_Click(object sender, RoutedEventArgs e)
		{
		}
	}
}

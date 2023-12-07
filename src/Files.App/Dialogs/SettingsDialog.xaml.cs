// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Views.Settings;
using Files.Core.ViewModels.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace Files.App.Dialogs
{
	public sealed partial class SettingsDialog : ContentDialog, IDialog<SettingsDialogViewModel>
	{
		public SettingsDialogViewModel ViewModel { get; set; }

		private FrameworkElement RootAppElement
			=> (FrameworkElement)MainWindow.Instance.Content;

		public SettingsDialog()
		{
			InitializeComponent();

			MainWindow.Instance.SizeChanged += Current_SizeChanged;
			UpdateDialogLayout();
		}

		public new async Task<DialogResult> ShowAsync()
		{
			return (DialogResult)await base.ShowAsync();
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
				2 => SettingsContentFrame.Navigate(typeof(FoldersPage)),
				3 => SettingsContentFrame.Navigate(typeof(TagsPage)),
				4 => SettingsContentFrame.Navigate(typeof(GitPage)),
				5 => SettingsContentFrame.Navigate(typeof(AdvancedPage)),
				6 => SettingsContentFrame.Navigate(typeof(AboutPage)),
				_ => SettingsContentFrame.Navigate(typeof(AppearancePage))
			};
		}

		private void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
		{
			MainWindow.Instance.SizeChanged -= Current_SizeChanged;
		}

		private void CloseSettingsDialogButton_Click(object sender, RoutedEventArgs e)
		{
			Hide();
		}
	}
}

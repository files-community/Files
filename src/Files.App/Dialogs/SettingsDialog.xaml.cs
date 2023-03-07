using Files.App.Views.Settings;
using Files.Backend.ViewModels.Dialogs;
using Files.Shared.Enums;
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
			=> (FrameworkElement)App.Window.Content;

		public SettingsDialog()
		{
			InitializeComponent();

			App.Window.SizeChanged += Current_SizeChanged;
			UpdateDialogLayout();
		}

		public new async Task<DialogResult> ShowAsync()
			=> (DialogResult)await base.ShowAsync();

		private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
			=> UpdateDialogLayout();

		private void UpdateDialogLayout()
		{
			ContainerGrid.Height = App.Window.Bounds.Height <= 760 ? App.Window.Bounds.Height - 70 : 690;
			ContainerGrid.Width = App.Window.Bounds.Width <= 1100 ? App.Window.Bounds.Width : 1100;
			MainSettingsNavigationView.PaneDisplayMode = App.Window.Bounds.Width < 700 ? NavigationViewPaneDisplayMode.LeftCompact : NavigationViewPaneDisplayMode.Left;
		}

		private void MainSettingsNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
		{
			var selectedItem = (NavigationViewItem)args.SelectedItem;
			int selectedItemTag = Convert.ToInt32(selectedItem.Tag);

			_ = selectedItemTag switch
			{
				0 => SettingsContentFrame.Navigate(typeof(AppearancePage)),
				1 => SettingsContentFrame.Navigate(typeof(PreferencesPage)),
				2 => SettingsContentFrame.Navigate(typeof(FoldersPage)),
				3 => SettingsContentFrame.Navigate(typeof(TagsPage)),
				4 => SettingsContentFrame.Navigate(typeof(AdvancedPage)),
				5 => SettingsContentFrame.Navigate(typeof(AboutPage)),
				_ => SettingsContentFrame.Navigate(typeof(AppearancePage))
			};
		}

		private void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
		{
			App.Window.SizeChanged -= Current_SizeChanged;
		}

		private void CloseSettingsDialogButton_Click(object sender, RoutedEventArgs e)
			=> Hide();
	}
}

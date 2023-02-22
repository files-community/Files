using Files.App.SettingsPages;
using Files.Core.ViewModels.Dialogs;
using Files.Core.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace Files.App.Dialogs
{
	public sealed partial class SettingsDialog : ContentDialog, IDialog<SettingsDialogViewModel>
	{
		public SettingsDialogViewModel ViewModel
		{
			get => (SettingsDialogViewModel)DataContext;
			set => DataContext = value;
		}

		// for some reason the requested theme wasn't being set on the content dialog, so this is used to manually bind to the requested app theme
		private FrameworkElement RootAppElement => App.Window.Content as FrameworkElement;

		public SettingsDialog()
		{
			InitializeComponent();
			SettingsPane.SelectedItem = SettingsPane.MenuItems[0];
			App.Window.SizeChanged += Current_SizeChanged;
			UpdateDialogLayout();
		}

		public new async Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();

		private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
		{
			UpdateDialogLayout();
		}

		private void UpdateDialogLayout()
		{
			ContainerGrid.Height = App.Window.Bounds.Height <= 760 ? App.Window.Bounds.Height - 70 : 690;
			ContainerGrid.Width = App.Window.Bounds.Width <= 1100 ? App.Window.Bounds.Width : 1100;
			SettingsPane.PaneDisplayMode = App.Window.Bounds.Width < 700 ? NavigationViewPaneDisplayMode.LeftCompact : NavigationViewPaneDisplayMode.Left;
		}

		private void SettingsPane_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
		{
			var selectedItem = (NavigationViewItem)args.SelectedItem;
			int selectedItemTag = Convert.ToInt32(selectedItem.Tag);

			_ = selectedItemTag switch
			{
				0 => SettingsContentFrame.Navigate(typeof(Appearance)),
				1 => SettingsContentFrame.Navigate(typeof(Preferences)),
				2 => SettingsContentFrame.Navigate(typeof(Folders)),
				3 => SettingsContentFrame.Navigate(typeof(Tags)),
				4 => SettingsContentFrame.Navigate(typeof(Advanced)),
				5 => SettingsContentFrame.Navigate(typeof(About)),
				_ => SettingsContentFrame.Navigate(typeof(Appearance))
			};
		}

		private void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
		{
			App.Window.SizeChanged -= Current_SizeChanged;
		}

		private void ButtonClose_Click(object sender, RoutedEventArgs e)
		{
			Hide();
		}
	}
}
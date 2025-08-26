// Copyright (c) Files Community
// Licensed under the MIT License.
using CommunityToolkit.WinUI;
using Files.App.Controls;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.AI.Actions.Hosting;
using Windows.System;
using Windows.UI.Core;
namespace Files.App.UserControls
{
	public sealed partial class NavigationToolbar : UserControl
	{
		// Dependency injections
		private readonly IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private readonly MainPageViewModel MainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();
		private readonly ICommandManager Commands = Ioc.Default.GetRequiredService<ICommandManager>();
		private readonly StatusCenterViewModel OngoingTasksViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

		// Properties
		[GeneratedDependencyProperty]
		public partial bool IsSidebarPaneOpenToggleButtonVisible { get; set; }

		[GeneratedDependencyProperty]
		public partial bool ShowOngoingTasks { get; set; }

		[GeneratedDependencyProperty]
		public partial bool ShowSettingsButton { get; set; }

		[GeneratedDependencyProperty]
		public partial NavigationToolbarViewModel ViewModel { get; set; }

		// Constructor
		public NavigationToolbar()
		{
			InitializeComponent();
		}

		private void OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			UpdateToolbarLayout();
		}

		private void Omnibar_Loaded(object sender, RoutedEventArgs e)
		{
			UpdateOmnibarPlaceholder();
		}

		private void UpdateOmnibarPlaceholder()
		{
			var placeholder = "Type here to search or search with Copilot";
			if (Omnibar != null)
			{
				Omnibar.PlaceholderText = placeholder;
			}
		}

		private void UpdateToolbarLayout()
		{
			if (ViewModel is null)
				return;

			// Check how much space the toolbar has available
			var availableWidth = ActualWidth;
			if (double.IsNaN(availableWidth) || availableWidth == 0)
				return;

			// Calculate the minimum required width based on visible elements
			var minRequiredWidth = CalculateMinimumRequiredWidth();

			// If we don't have enough space, move items to the overflow menu
			var shouldShowOverflow = availableWidth < minRequiredWidth;
			NavigationButtonOverflowFlyoutButton.Visibility = shouldShowOverflow ? Visibility.Visible : Visibility.Collapsed;
		}

		private double CalculateMinimumRequiredWidth()
		{
			// This would calculate the minimum width needed for all visible elements
			// For now, return a reasonable estimate
			return 400; // pixels
		}

		private void BackButton_Click(object sender, RoutedEventArgs e)
		{
			Commands.ExecuteCommand("NavigateBack");
		}

		private void ForwardButton_Click(object sender, RoutedEventArgs e)
		{
			Commands.ExecuteCommand("NavigateForward");
		}

		private void UpButton_Click(object sender, RoutedEventArgs e)
		{
			Commands.ExecuteCommand("NavigateUp");
		}

		private void RefreshButton_Click(object sender, RoutedEventArgs e)
		{
			Commands.ExecuteCommand("RefreshItems");
		}

		private void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
		{
			ViewModel.BreadcrumbBar_ItemClicked(sender, args);
		}

		private void NavigateToPath(string path)
		{
			ViewModel.NavigateToPath(path);
		}

		private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
			{
				ViewModel.SearchBox_TextChanged(sender, args);
			}
		}

		private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
		{
			ViewModel.SearchBox_QuerySubmitted(sender, args);
		}

		private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
		{
			ViewModel.SearchBox_SuggestionChosen(sender, args);
		}

		private void Omnibar_GotFocus(object sender, RoutedEventArgs e)
		{
			// When Omnibar gains focus, switch to Address Mode to show TextBox
			Omnibar.CurrentSelectedMode = OmnibarAddressMode;
		}

		private void Omnibar_LostFocus(object sender, RoutedEventArgs e)
		{
			// When Omnibar loses focus, revert to Path Mode to display BreadcrumbBar
			Omnibar.CurrentSelectedMode = OmnibarPathMode;
		}

		private async void Omnibar_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key is VirtualKey.Escape)
			{
				Omnibar.IsFocused = false;
				ContentPageContext.ShellPage!.PaneHolder.FocusActivePane();
			}
			else if (e.Key is VirtualKey.Tab && Omnibar.IsFocused && !InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
			{
				var currentSelectedMode = Omnibar.CurrentSelectedMode;
				Omnibar.IsFocused = false;
				await Task.Delay(15);
				if (currentSelectedMode == OmnibarPathMode)
					BreadcrumbBar.Focus(FocusState.Keyboard);
				else if (currentSelectedMode == OmnibarCommandPaletteMode)
					OmnibarCommandPaletteMode.Focus(FocusState.Keyboard);
				else if (currentSelectedMode == OmnibarSearchMode)
					OmnibarSearchMode.Focus(FocusState.Keyboard);
			}
		}

		private void NavigationButtonOverflowFlyoutButton_LosingFocus(UIElement sender, LosingFocusEventArgs args)
		{
			// Prevent the Omnibar from taking focus if the overflow button is hidden while the button is focused
			if (args.NewFocusedElement is TextBox)
				args.Cancel = true;
		}

		private void BreadcrumbBarItem_DragLeave(object sender, DragEventArgs e)
		{
			ViewModel.PathBoxItem_DragLeave(sender, e);
		}

		private async void BreadcrumbBarItem_DragOver(object sender, DragEventArgs e)
		{
			await ViewModel.PathBoxItem_DragOver(sender, e);
		}

		private async void BreadcrumbBarItem_Drop(object sender, DragEventArgs e)
		{
			await ViewModel.PathBoxItem_Drop(sender, e);
		}

		private async void BreadcrumbBar_ItemMiddleClicked(object sender, BreadcrumbBarItemClickedEventArgs args)
		{
			// Don't handle middle click on the current folder (last item)
			if (args.Index >= ViewModel.PathComponents.Count - 1)
				return;

			// Get the path from the clicked breadcrumb item
			var path = ViewModel.PathComponents[args.Index].Path;

			// Open the parent folder in a new tab
			await NavigationHelpers.AddNewTabByPathAsync(typeof(ShellPanesPage), path, false);
		}
	}
}

using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Helpers.XamlHelpers;
using Files.App.ViewModels;
using Files.Backend.Services.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Windows.Input;
using Windows.System;
using FocusManager = Microsoft.UI.Xaml.Input.FocusManager;

namespace Files.App.UserControls
{
	public sealed partial class AddressToolbar : UserControl
	{
		public IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public ISearchBox SearchBox => ViewModel.SearchBox;

		public AddressToolbar()
		{
			InitializeComponent();
			Loading += NavigationToolbar_Loading;
		}

		private void NavigationToolbar_Loading(FrameworkElement sender, object args)
		{
			OngoingTasksViewModel.ProgressBannerPosted += OngoingTasksActions_ProgressBannerPosted;
		}

		private void VisiblePath_Loaded(object sender, RoutedEventArgs e)
		{
			// AutoSuggestBox won't receive focus unless it's fully loaded
			VisiblePath.Focus(FocusState.Programmatic);
			DependencyObjectHelpers.FindChild<TextBox>(VisiblePath)?.SelectAll();
		}

		private void ManualPathEntryItem_Click(object sender, PointerRoutedEventArgs e)
		{
			if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
			{
				var ptrPt = e.GetCurrentPoint(NavToolbar);
				if (ptrPt.Properties.IsMiddleButtonPressed)
				{
					return;
				}
			}
			ViewModel.IsEditModeEnabled = true;
		}

		private void VisiblePath_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Escape)
			{
				ViewModel.IsEditModeEnabled = false;
			}
		}

		private void VisiblePath_LostFocus(object sender, RoutedEventArgs e)
		{
			if (FocusManager.GetFocusedElement() is FlyoutBase ||
				FocusManager.GetFocusedElement() is AppBarButton ||
				FocusManager.GetFocusedElement() is Popup)
			{
				return;
			}

			var element = FocusManager.GetFocusedElement();
			var elementAsControl = element as Control;
			if (elementAsControl is null)
				return;

			if (elementAsControl.FocusState != FocusState.Programmatic && elementAsControl.FocusState != FocusState.Keyboard)
			{
				ViewModel.IsEditModeEnabled = false;
			}
			else if (ViewModel.IsEditModeEnabled)
			{
				this.VisiblePath.Focus(FocusState.Programmatic);
			}
		}

		private void SearchButton_Click(object sender, RoutedEventArgs e) => ViewModel.SwitchSearchBoxVisibility();

		private void SearchBox_Escaped(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) => ViewModel.CloseSearchBox();

		private void SearchRegion_OnGotFocus(object sender, RoutedEventArgs e) => ViewModel.SearchRegion_GotFocus(sender, e);

		private void SearchRegion_LostFocus(object sender, RoutedEventArgs e) => ViewModel.SearchRegion_LostFocus(sender, e);

		private void VisiblePath_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => ViewModel.VisiblePath_QuerySubmitted(sender, args);

		public OngoingTasksViewModel OngoingTasksViewModel { get; set; }

		private void OngoingTasksActions_ProgressBannerPosted(object sender, PostedStatusBanner e)
		{
			// Displays a teaching tip the first time a banner is posted
			if (UserSettingsService.AppSettingsService.ShowStatusCenterTeachingTip)
			{
				StatusCenterTeachingTip.IsOpen = true;
				UserSettingsService.AppSettingsService.ShowStatusCenterTeachingTip = false;
			}
		}

		// Using a DependencyProperty as the backing store for ShowOngoingTasks.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ShowOngoingTasksProperty =
			DependencyProperty.Register(nameof(ShowOngoingTasks), typeof(bool), typeof(AddressToolbar), new PropertyMetadata(null));
		public bool ShowOngoingTasks
		{
			get => (bool)GetValue(ShowOngoingTasksProperty);
			set => SetValue(ShowOngoingTasksProperty, value);
		}

		// Using a DependencyProperty as the backing store for ShowSettingsButton.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ShowSettingsButtonProperty =
			DependencyProperty.Register(nameof(ShowSettingsButton), typeof(bool), typeof(AddressToolbar), new PropertyMetadata(null));
		public bool ShowSettingsButton
		{
			get => (bool)GetValue(dp: ShowSettingsButtonProperty);
			set => SetValue(ShowSettingsButtonProperty, value);
		}

		// Using a DependencyProperty as the backing store for CollapseSearchBox.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ShowSearchBoxProperty =
			DependencyProperty.Register(nameof(ShowSearchBox), typeof(bool), typeof(AddressToolbar), new PropertyMetadata(null));
		public bool ShowSearchBox
		{
			get { return (bool)GetValue(ShowSearchBoxProperty); }
			set { SetValue(ShowSearchBoxProperty, value); }
		}

		public static readonly DependencyProperty SettingsButtonCommandProperty =
			DependencyProperty.Register(nameof(SettingsButtonCommand), typeof(ICommand), typeof(AddressToolbar), new PropertyMetadata(null));
		public ICommand SettingsButtonCommand
		{
			get => (ICommand)GetValue(SettingsButtonCommandProperty);
			set => SetValue(SettingsButtonCommandProperty, value);
		}

		public static readonly DependencyProperty CanPasteInPageProperty =
			DependencyProperty.Register("CanPasteInPage", typeof(bool), typeof(AddressToolbar), new PropertyMetadata(null));
		public bool CanPasteInPage
		{
			get => (bool)GetValue(dp: CanPasteInPageProperty);
			set => SetValue(CanPasteInPageProperty, value);
		}

		// Using a DependencyProperty as the backing store for ViewModel.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register(nameof(ViewModel), typeof(ToolbarViewModel), typeof(AddressToolbar), new PropertyMetadata(null));
		public ToolbarViewModel ViewModel
		{
			get => (ToolbarViewModel)GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}
	}
}

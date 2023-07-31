using Microsoft.UI.Xaml;

namespace Files.App.UserControls.Sidebar
{
	public sealed partial class SidebarView
	{
		public SidebarDisplayMode DisplayMode
		{
			get { return (SidebarDisplayMode)GetValue(DisplayModeProperty); }
			set { SetValue(DisplayModeProperty, value); }
		}
		public static readonly DependencyProperty DisplayModeProperty =
			DependencyProperty.Register("DisplayMode", typeof(SidebarDisplayMode), typeof(SidebarView), new PropertyMetadata(SidebarDisplayMode.Expanded, OnPropertyChanged));

		public UIElement InnerContent
		{
			get { return (UIElement)GetValue(InnerContentProperty); }
			set { SetValue(InnerContentProperty, value); }
		}
		public static readonly DependencyProperty InnerContentProperty =
			DependencyProperty.Register("InnerContent", typeof(UIElement), typeof(SidebarView), new PropertyMetadata(null));

		public bool IsPaneOpen
		{
			get { return (bool)GetValue(IsPaneOpenProperty); }
			set { SetValue(IsPaneOpenProperty, value); }
		}
		public static readonly DependencyProperty IsPaneOpenProperty =
			DependencyProperty.Register("IsPaneOpen", typeof(bool), typeof(SidebarView), new PropertyMetadata(false, OnPropertyChanged));

		public double OpenPaneWidth
		{
			get { return (double)GetValue(OpenPaneWidthProperty); }
			set { SetValue(OpenPaneWidthProperty, value); }
		}
		public static readonly DependencyProperty OpenPaneWidthProperty =
			DependencyProperty.Register("OpenPaneWidth", typeof(double), typeof(SidebarView), new PropertyMetadata(240d, OnPropertyChanged));

		public ISidebarViewModel ViewModel
		{
			get => (ISidebarViewModel)GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register(nameof(ViewModel), typeof(ISidebarViewModel), typeof(SidebarView), new PropertyMetadata(null));

		public ISidebarItemModel SelectedItem
		{
			get => (ISidebarItemModel)GetValue(SelectedItemProperty);
			set
			{
				SetValue(SelectedItemProperty, value);
			}
		}
		public static readonly DependencyProperty SelectedItemProperty =
			DependencyProperty.Register(nameof(SelectedItem), typeof(ISidebarItemModel), typeof(SidebarControl), new PropertyMetadata(null));

		public static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is not SidebarView control) return;

			if (e.Property == OpenPaneWidthProperty)
			{
				control.UpdateOpenPaneLengthColumn();
			}
			else if (e.Property == DisplayModeProperty)
			{
				control.UpdateDisplayMode();
			}
			else if (e.Property == IsPaneOpenProperty)
			{
				control.UpdateMinimalMode();
			}
		}
	}
}

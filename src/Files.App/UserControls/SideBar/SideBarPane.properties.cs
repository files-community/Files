using Microsoft.UI.Xaml;

namespace Files.App.UserControls.SideBar
{
	public enum SideBarDisplayMode
	{
		Minimal,
		Compact,
		Expanded
	}

	public sealed partial class SideBarPane
	{
		public object Items
		{
			get { return (object)GetValue(ItemsProperty); }
			set { SetValue(ItemsProperty, value); }
		}
		public static readonly DependencyProperty ItemsProperty =
			DependencyProperty.Register("Items", typeof(object), typeof(SideBarPane), new PropertyMetadata(null));

		public SideBarDisplayMode DisplayMode
		{
			get { return (SideBarDisplayMode)GetValue(DisplayModeProperty); }
			set { SetValue(DisplayModeProperty, value); }
		}
		public event EventHandler<SideBarDisplayMode>? DisplayModeChanged;
		public static readonly DependencyProperty DisplayModeProperty =
			DependencyProperty.Register("DisplayMode", typeof(SideBarDisplayMode), typeof(SideBarPane), new PropertyMetadata(SideBarDisplayMode.Expanded, OnPropertyChanged));

		public UIElement InnerContent
		{
			get { return (UIElement)GetValue(InnerContentProperty); }
			set { SetValue(InnerContentProperty, value); }
		}
		public static readonly DependencyProperty InnerContentProperty =
			DependencyProperty.Register("InnerContent", typeof(UIElement), typeof(SideBarPane), new PropertyMetadata(null));

		public INavigationControlItem SelectedItem
		{
			get { return (INavigationControlItem)GetValue(SelectedItemProperty); }
			set { SetValue(SelectedItemProperty, value); }
		}
		public static readonly DependencyProperty SelectedItemProperty =
			DependencyProperty.Register("SelectedItem", typeof(INavigationControlItem), typeof(SideBarPane), new PropertyMetadata(null));

		public bool IsPaneOpen
		{
			get { return (bool)GetValue(IsPaneOpenProperty); }
			set { SetValue(IsPaneOpenProperty, value); }
		}
		public static readonly DependencyProperty IsPaneOpenProperty =
			DependencyProperty.Register("IsPaneOpen", typeof(bool), typeof(SideBarPane), new PropertyMetadata(false, OnPropertyChanged));


		public static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is not SideBarPane control) return;

			if (e.Property == DisplayModeProperty)
			{
				control.UpdateDisplayMode();
				control.DisplayModeChanged?.Invoke(control, (SideBarDisplayMode)e.NewValue);
			}
			else if (e.Property == IsPaneOpenProperty)
			{
				control.UpdateMinimalMode();
			}
		}
	}
}

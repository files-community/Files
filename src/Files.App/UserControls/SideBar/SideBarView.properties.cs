using Microsoft.UI.Xaml;

namespace Files.App.UserControls.SideBar
{
	public sealed partial class SideBarView
	{
		public object Items
		{
			get { return (object)GetValue(ItemsProperty); }
			set { SetValue(ItemsProperty, value); }
		}
		public static readonly DependencyProperty ItemsProperty =
			DependencyProperty.Register("Items", typeof(object), typeof(SideBarView), new PropertyMetadata(null));

		public SideBarDisplayMode DisplayMode
		{
			get { return (SideBarDisplayMode)GetValue(DisplayModeProperty); }
			set { SetValue(DisplayModeProperty, value); }
		}
		public static readonly DependencyProperty DisplayModeProperty =
			DependencyProperty.Register("DisplayMode", typeof(SideBarDisplayMode), typeof(SideBarView), new PropertyMetadata(SideBarDisplayMode.Expanded));

		public UIElement InnerContent
		{
			get { return (UIElement)GetValue(InnerContentProperty); }
			set { SetValue(InnerContentProperty, value); }
		}
		public static readonly DependencyProperty InnerContentProperty =
			DependencyProperty.Register("InnerContent", typeof(UIElement), typeof(SideBarView), new PropertyMetadata(null));

		public UIElement TabContent
		{
			get => (UIElement)GetValue(TabContentProperty);
			set => SetValue(TabContentProperty, value);
		}
		public static readonly DependencyProperty TabContentProperty =
			DependencyProperty.Register(nameof(TabContent), typeof(UIElement), typeof(SideBarView), new PropertyMetadata(null));

		public bool IsPaneOpen
		{
			get { return (bool)GetValue(IsPaneOpenProperty); }
			set { SetValue(IsPaneOpenProperty, value); }
		}
		public static readonly DependencyProperty IsPaneOpenProperty =
			DependencyProperty.Register("IsPaneOpen", typeof(bool), typeof(SideBarView), new PropertyMetadata(false));

		public ISideBarViewModel ViewModel
		{
			get => (ISideBarViewModel)GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register(nameof(ViewModel), typeof(ISideBarViewModel), typeof(SideBarView), new PropertyMetadata(null));

		public INavigationControlItem SelectedItem
		{
			get => (INavigationControlItem)GetValue(SelectedItemProperty);
			set
			{
				if (IsLoaded)
					SetValue(SelectedItemProperty, value);
			}
		}
		public static readonly DependencyProperty SelectedItemProperty = 
			DependencyProperty.Register(nameof(SelectedItem), typeof(INavigationControlItem), typeof(SidebarControl), new PropertyMetadata(null));
	}
}

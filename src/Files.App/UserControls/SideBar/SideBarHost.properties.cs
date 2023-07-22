using Microsoft.UI.Xaml;

namespace Files.App.UserControls.SideBar
{
	public sealed partial class SideBarHost
	{
		public object Items
		{
			get { return (object)GetValue(ItemsProperty); }
			set { SetValue(ItemsProperty, value); }
		}
		public static readonly DependencyProperty ItemsProperty =
			DependencyProperty.Register("Items", typeof(object), typeof(SideBarHost), new PropertyMetadata(null));

		public SideBarDisplayMode DisplayMode
		{
			get { return (SideBarDisplayMode)GetValue(DisplayModeProperty); }
			set { SetValue(DisplayModeProperty, value); }
		}
		public event EventHandler<SideBarDisplayMode>? DisplayModeChanged;
		public static readonly DependencyProperty DisplayModeProperty =
			DependencyProperty.Register("DisplayMode", typeof(SideBarDisplayMode), typeof(SideBarPane), new PropertyMetadata(SideBarDisplayMode.Expanded));

		public UIElement InnerContent
		{
			get { return (UIElement)GetValue(InnerContentProperty); }
			set { SetValue(InnerContentProperty, value); }
		}
		public static readonly DependencyProperty InnerContentProperty =
			DependencyProperty.Register("InnerContent", typeof(UIElement), typeof(SideBarHost), new PropertyMetadata(null));

		public object SelectedItem
		{
			get { return (object)GetValue(SelectedItemProperty); }
			set { SetValue(SelectedItemProperty, value); }
		}
		public static readonly DependencyProperty SelectedItemProperty =
			DependencyProperty.Register("SelectedItem", typeof(object), typeof(SideBarHost), new PropertyMetadata(null));

		public UIElement TabContent
		{
			get => (UIElement)GetValue(TabContentProperty);
			set => SetValue(TabContentProperty, value);
		}
		public static readonly DependencyProperty TabContentProperty = DependencyProperty.Register(nameof(TabContent), typeof(UIElement), typeof(SideBarHost), new PropertyMetadata(null));
	}
}

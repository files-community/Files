using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.SideBar
{
	public sealed partial class SideBarItem : Control
	{
		public SideBarPane Owner
		{
			get { return (SideBarPane)GetValue(OwnerProperty); }
			set { SetValue(OwnerProperty, value); }
		}
		public static readonly DependencyProperty OwnerProperty =
			DependencyProperty.Register("Owner", typeof(SideBarPane), typeof(SideBarItem), new PropertyMetadata(null));

		public bool IsSelected
		{
			get { return (bool)GetValue(IsSelectedProperty); }
			set { SetValue(IsSelectedProperty, value); }
		}
		public static readonly DependencyProperty IsSelectedProperty =
			DependencyProperty.Register("IsSelected", typeof(bool), typeof(SideBarItem), new PropertyMetadata(false, OnPropertyChanged));

		public bool IsExpanded
		{
			get { return (bool)GetValue(IsExpandedProperty); }
			set { SetValue(IsExpandedProperty, value); }
		}
		public static readonly DependencyProperty IsExpandedProperty =
			DependencyProperty.Register("IsExpanded", typeof(bool), typeof(SideBarItem), new PropertyMetadata(false, OnPropertyChanged));

		public INavigationControlItem? Item
		{
			get { return (INavigationControlItem)GetValue(ItemProperty); }
			set { SetValue(ItemProperty, value); }
		}
		public static readonly DependencyProperty ItemProperty =
			DependencyProperty.Register("Item", typeof(INavigationControlItem), typeof(SideBarItem), new PropertyMetadata(null));

		public bool UseReorderDrop
		{
			get { return (bool)GetValue(UseReorderDropProperty); }
			set { SetValue(UseReorderDropProperty, value); }
		}
		public static readonly DependencyProperty UseReorderDropProperty =
			DependencyProperty.Register("UseReorderDrop", typeof(bool), typeof(SideBarItem), new PropertyMetadata(false));

		public FrameworkElement Icon
		{
			get { return (FrameworkElement)GetValue(IconProperty); }
			set { SetValue(IconProperty, value); }
		}
		public static readonly DependencyProperty IconProperty =
			DependencyProperty.Register("Icon", typeof(FrameworkElement), typeof(SideBarItem), new PropertyMetadata(null));

		public SideBarDisplayMode DisplayMode
		{
			get { return (SideBarDisplayMode)GetValue(DisplayModeProperty); }
			set { SetValue(DisplayModeProperty, value); }
		}
		public static readonly DependencyProperty DisplayModeProperty =
			DependencyProperty.Register("DisplayMode", typeof(SideBarDisplayMode), typeof(SideBarItem), new PropertyMetadata(SideBarDisplayMode.Expanded, OnPropertyChanged));

		public static void SetTemplateRoot(DependencyObject target, FrameworkElement value)
		{
			target.SetValue(TemplateRootProperty, value);
		}
		public static FrameworkElement GetTemplateRoot(DependencyObject target)
		{
			return (FrameworkElement)target.GetValue(TemplateRootProperty);
		}
		public static readonly DependencyProperty TemplateRootProperty =
			DependencyProperty.Register("TemplateRoot", typeof(FrameworkElement), typeof(FrameworkElement), new PropertyMetadata(null));

		public static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is not SideBarItem item) return;
			if (e.Property == DisplayModeProperty)
			{
				item.SideBarDisplayModeChanged((SideBarDisplayMode)e.NewValue);
			}
			else if (e.Property == IsSelectedProperty)
			{
				item.UpdateSelectionState();
			}
			else if (e.Property == IsExpandedProperty)
			{
				item.UpdateExpansionState();
			}
			else if(e.Property == ItemProperty)
			{
				item.HookupIconChangeListener(e.OldValue as INavigationControlItem, e.NewValue as INavigationControlItem);
				item.UpdateExpansionState();
				item.ReevaluateSelection();
			}
			else if(e.Property == DataContextProperty)
			{
				item.ReevaluateSelection();
			}
		}
	}
}

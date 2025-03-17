// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;

namespace Files.App.Controls
{
	public sealed partial class SidebarItem
	{
		[GeneratedDependencyProperty]
		public partial SidebarView? Owner { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsSelected { get; set; }

		[GeneratedDependencyProperty(DefaultValue = true)]
		public partial bool IsExpanded { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsInFlyout { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 30d)]
		public partial double ChildrenPresenterHeight { get; set; }

		[GeneratedDependencyProperty]
		public partial ISidebarItemModel? Item { get; set; }

		[GeneratedDependencyProperty]
		public partial bool UseReorderDrop { get; set; }

		[GeneratedDependencyProperty]
		public partial FrameworkElement? Icon { get; set; }

		[GeneratedDependencyProperty]
		public partial FrameworkElement? Decorator { get; set; }

		[GeneratedDependencyProperty(DefaultValue = SidebarDisplayMode.Expanded)]
		public partial SidebarDisplayMode DisplayMode { get; set; }

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

		partial void OnDisplayModePropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			SidebarDisplayModeChanged((SidebarDisplayMode)e.OldValue);
		}

		partial void OnIsSelectedChanged(bool newValue)
		{
			UpdateSelectionState();
		}

		partial void OnIsExpandedChanged(bool newValue)
		{
			UpdateExpansionState();
		}

		partial void OnItemChanged(ISidebarItemModel? newValue)
		{
			HandleItemChange();
		}
	}
}

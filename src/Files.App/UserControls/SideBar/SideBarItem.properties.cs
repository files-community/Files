// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.Sidebar
{
	public sealed partial class SidebarItem : Control
	{
		public SidebarView? Owner
		{
			get => (SidebarView?)GetValue(OwnerProperty);
			set => SetValue(OwnerProperty, value);
		}

		public string Text
		{
			get => (string)GetValue(TextProperty);
			set => SetValue(TextProperty, value);
		}

		public string TooltipText
		{
			get => (string)GetValue(TooltipTextProperty);
			set => SetValue(TooltipTextProperty, value);
		}

		public bool IsSelected
		{
			get => (bool)GetValue(IsSelectedProperty);
			set => SetValue(IsSelectedProperty, value);
		}

		public bool IsExpanded
		{
			get => (bool)GetValue(IsExpandedProperty);
			set => SetValue(IsExpandedProperty, value);
		}

		public bool IsInFlyout
		{
			get => (bool)GetValue(IsInFlyoutProperty);
			set => SetValue(IsInFlyoutProperty, value);
		}

		public double ChildrenPresenterHeight
		{
			get => (double)GetValue(ChildrenPresenterHeightProperty);
			set => SetValue(ChildrenPresenterHeightProperty, value);
		}

		public INavigationControlItem? Item
		{
			get => (INavigationControlItem)GetValue(ItemProperty);
			set => SetValue(ItemProperty, value);
		}

		public bool UseReorderDrop
		{
			get => (bool)GetValue(UseReorderDropProperty);
			set => SetValue(UseReorderDropProperty, value);
		}

		public FrameworkElement? Icon
		{
			get => (FrameworkElement?)GetValue(IconProperty);
			set => SetValue(IconProperty, value);
		}

		public FrameworkElement? Decorator
		{
			get => (FrameworkElement?)GetValue(DecoratorProperty);
			set => SetValue(DecoratorProperty, value);
		}

		public SidebarDisplayMode DisplayMode
		{
			get => (SidebarDisplayMode)GetValue(DisplayModeProperty);
			set => SetValue(DisplayModeProperty, value);
		}

		public static readonly DependencyProperty OwnerProperty =
			DependencyProperty.Register(
				nameof(Owner),
				typeof(SidebarView),
				typeof(SidebarItem),
				new PropertyMetadata(null));

		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register(
				nameof(Text),
				typeof(string),
				typeof(SidebarItem),
				new PropertyMetadata(string.Empty));

		public static readonly DependencyProperty TooltipTextProperty =
			DependencyProperty.Register(
				nameof(TooltipText),
				typeof(string),
				typeof(SidebarItem),
				new PropertyMetadata(string.Empty));

		public static readonly DependencyProperty IsSelectedProperty =
			DependencyProperty.Register(
				nameof(IsSelected),
				typeof(bool),
				typeof(SidebarItem),
				new PropertyMetadata(false, OnPropertyChanged));

		public static readonly DependencyProperty IsExpandedProperty =
			DependencyProperty.Register(
				nameof(IsExpanded),
				typeof(bool),
				typeof(SidebarItem),
				new PropertyMetadata(true, OnPropertyChanged));

		public static readonly DependencyProperty IsInFlyoutProperty =
			DependencyProperty.Register(
				nameof(IsInFlyout),
				typeof(bool),
				typeof(SidebarItem),
				new PropertyMetadata(false));

		public static readonly DependencyProperty ChildrenPresenterHeightProperty =
			DependencyProperty.Register(
				nameof(ChildrenPresenterHeight),
				typeof(double),
				typeof(SidebarItem),
				new PropertyMetadata(30d)); 

		public static readonly DependencyProperty ItemProperty =
			DependencyProperty.Register(
				nameof(Item),
				typeof(INavigationControlItem),
				typeof(SidebarItem),
				new PropertyMetadata(null));

		public static readonly DependencyProperty UseReorderDropProperty =
			DependencyProperty.Register(
				nameof(UseReorderDrop),
				typeof(bool),
				typeof(SidebarItem),
				new PropertyMetadata(false));

		public static readonly DependencyProperty IconProperty =
			DependencyProperty.Register(
				nameof(Icon),
				typeof(FrameworkElement),
				typeof(SidebarItem),
				new PropertyMetadata(null));

		public static readonly DependencyProperty DecoratorProperty =
			DependencyProperty.Register(nameof(Decorator), typeof(FrameworkElement), typeof(SidebarItem), new PropertyMetadata(null));

		public static readonly DependencyProperty DisplayModeProperty =
			DependencyProperty.Register(
				nameof(DisplayMode),
				typeof(SidebarDisplayMode),
				typeof(SidebarItem),
				new PropertyMetadata(SidebarDisplayMode.Expanded, OnPropertyChanged));

		public static readonly DependencyProperty TemplateRootProperty =
			DependencyProperty.Register(
				"TemplateRoot",
				typeof(FrameworkElement),
				typeof(FrameworkElement),
				new PropertyMetadata(null));

		public static void SetTemplateRoot(DependencyObject target, FrameworkElement value)
			=> target.SetValue(TemplateRootProperty, value);
		public static FrameworkElement GetTemplateRoot(DependencyObject target)
			=> (FrameworkElement)target.GetValue(TemplateRootProperty);

		public static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is not SidebarItem item)
				return;

			if (e.Property == DisplayModeProperty)
			{
				item.SidebarDisplayModeChanged((SidebarDisplayMode)e.OldValue);
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
				item.HandleItemChange();
			}
			else
			{
				Debug.Write($@"The property ""{e.Property}"" has been changed but not updated accordingly in SidebarItem.");
			}
		}
	}
}

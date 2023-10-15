// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.SideBar
{
	public sealed partial class SideBarItem : Control
	{
		public SideBarView? Owner
		{
			get => (SideBarView?)GetValue(OwnerProperty);
			set => SetValue(OwnerProperty, value);
		}

		public static readonly DependencyProperty OwnerProperty =
			DependencyProperty.Register(
				nameof(Owner),
				typeof(SideBarView),
				typeof(SideBarItem),
				new PropertyMetadata(null));

		public bool IsSelected
		{
			get => (bool)GetValue(IsSelectedProperty);
			set => SetValue(IsSelectedProperty, value);
		}

		public static readonly DependencyProperty IsSelectedProperty =
			DependencyProperty.Register(
				nameof(IsSelected),
				typeof(bool),
				typeof(SideBarItem),
				new PropertyMetadata(false, OnPropertyChanged));

		public bool IsExpanded
		{
			get => (bool)GetValue(IsExpandedProperty);
			set => SetValue(IsExpandedProperty, value);
		}

		public static readonly DependencyProperty IsExpandedProperty =
			DependencyProperty.Register(
				nameof(IsExpanded),
				typeof(bool),
				typeof(SideBarItem),
				new PropertyMetadata(true, OnPropertyChanged));

		public bool IsInFlyout
		{
			get => (bool)GetValue(IsInFlyoutProperty);
			set => SetValue(IsInFlyoutProperty, value);
		}

		public static readonly DependencyProperty IsInFlyoutProperty =
			DependencyProperty.Register(
				nameof(IsInFlyout),
				typeof(bool),
				typeof(SideBarItem),
				new PropertyMetadata(false));

		public double ChildrenPresenterHeight
		{
			get => (double)GetValue(ChildrenPresenterHeightProperty);
			set => SetValue(ChildrenPresenterHeightProperty, value);
		}

		// Using 30 as a default in case something goes wrong
		public static readonly DependencyProperty ChildrenPresenterHeightProperty =
			DependencyProperty.Register(
				nameof(ChildrenPresenterHeight),
				typeof(double),
				typeof(SideBarItem),
				new PropertyMetadata(30d)); 

		public ISideBarItemModel? Item
		{
			get => (ISideBarItemModel)GetValue(ItemProperty);
			set => SetValue(ItemProperty, value);
		}

		public static readonly DependencyProperty ItemProperty =
			DependencyProperty.Register(
				nameof(Item),
				typeof(ISideBarItemModel),
				typeof(SideBarItem),
				new PropertyMetadata(null));

		public bool UseReorderDrop
		{
			get => (bool)GetValue(UseReorderDropProperty);
			set => SetValue(UseReorderDropProperty, value);
		}

		public static readonly DependencyProperty UseReorderDropProperty =
			DependencyProperty.Register(
				nameof(UseReorderDrop),
				typeof(bool),
				typeof(SideBarItem),
				new PropertyMetadata(false));

		public FrameworkElement? Icon
		{
			get => (FrameworkElement?)GetValue(IconProperty);
			set => SetValue(IconProperty, value);
		}

		public static readonly DependencyProperty IconProperty =
			DependencyProperty.Register(
				nameof(Icon),
				typeof(FrameworkElement),
				typeof(SideBarItem),
				new PropertyMetadata(null));

		public FrameworkElement? Decorator
		{
			get => (FrameworkElement?)GetValue(DecoratorProperty);
			set => SetValue(DecoratorProperty, value);
		}

		public static readonly DependencyProperty DecoratorProperty =
			DependencyProperty.Register(
				nameof(Decorator),
				typeof(FrameworkElement),
				typeof(SideBarItem),
				new PropertyMetadata(null));

		public SideBarPaneDisplayMode DisplayMode
		{
			get => (SideBarPaneDisplayMode)GetValue(DisplayModeProperty);
			set => SetValue(DisplayModeProperty, value);
		}

		public static readonly DependencyProperty DisplayModeProperty =
			DependencyProperty.Register(
				nameof(DisplayMode),
				typeof(SideBarPaneDisplayMode),
				typeof(SideBarItem),
				new PropertyMetadata(SideBarPaneDisplayMode.Expanded, OnPropertyChanged));

		public static void SetTemplateRoot(DependencyObject target, FrameworkElement value)
		{
			target.SetValue(TemplateRootProperty, value);
		}

		public static FrameworkElement GetTemplateRoot(DependencyObject target)
		{
			return (FrameworkElement)target.GetValue(TemplateRootProperty);
		}

		public static readonly DependencyProperty TemplateRootProperty =
			DependencyProperty.Register(
				"TemplateRoot",
				typeof(FrameworkElement),
				typeof(FrameworkElement),
				new PropertyMetadata(null));

		public static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is not SideBarItem item) return;

			if (e.Property == DisplayModeProperty)
				item.SidebarDisplayModeChanged((SideBarPaneDisplayMode)e.OldValue);
			else if (e.Property == IsSelectedProperty)
				item.UpdateSelectionState();
			else if (e.Property == IsExpandedProperty)
				item.UpdateExpansionState();
			else if(e.Property == ItemProperty)
				item.HandleItemChange();
			else
				Debug.Write(e.Property.ToString());
		}
	}
}

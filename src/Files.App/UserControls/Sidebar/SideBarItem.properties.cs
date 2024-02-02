// Copyright (c) 2023 Files Community
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

		public static readonly DependencyProperty OwnerProperty =
			DependencyProperty.Register(
				nameof(Owner),
				typeof(SidebarView),
				typeof(SidebarItem),
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
				typeof(SidebarItem),
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
				typeof(SidebarItem),
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
				typeof(SidebarItem),
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
				typeof(SidebarItem),
				new PropertyMetadata(30d));

		public ISidebarItemModel? Item
		{
			get => (ISidebarItemModel)GetValue(ItemProperty);
			set => SetValue(ItemProperty, value);
		}

		public static readonly DependencyProperty ItemProperty =
			DependencyProperty.Register(
				nameof(Item),
				typeof(ISidebarItemModel),
				typeof(SidebarItem),
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
				typeof(SidebarItem),
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
				typeof(SidebarItem),
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
				typeof(SidebarItem),
				new PropertyMetadata(null));

		public SidebarDisplayMode DisplayMode
		{
			get => (SidebarDisplayMode)GetValue(DisplayModeProperty);
			set => SetValue(DisplayModeProperty, value);
		}

		public static readonly DependencyProperty DisplayModeProperty =
			DependencyProperty.Register(
				nameof(DisplayMode),
				typeof(SidebarDisplayMode),
				typeof(SidebarItem),
				new PropertyMetadata(SidebarDisplayMode.Expanded, OnPropertyChanged));

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
			if (sender is not SidebarItem item)
				return;

			switch (e.Property.ToString())
			{
				case nameof(DisplayModeProperty):
					item.SidebarDisplayModeChanged((SidebarDisplayMode)e.OldValue);
					break;
				case nameof(IsSelectedProperty):
					item.UpdateSelectionState();
					break;
				case nameof(IsExpandedProperty):
					item.UpdateExpansionState();
					break;
				case nameof(ItemProperty):
					item.HandleItemChange();
					break;
				default:
					Debug.Write(e.Property.ToString());
					break;
			}
		}
	}
}

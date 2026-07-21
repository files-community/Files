// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace Files.App.Controls;

internal static class TableViewCellEditingBehavior
{
	public static void Prepare(FrameworkElement editingElement)
	{
		editingElement.Loaded += EditingElement_Loaded;
		editingElement.LosingFocus += EditingElement_LosingFocus;
		editingElement.LostFocus += EditingElement_LostFocus;
		editingElement.KeyDown += EditingElement_KeyDown;
	}

	public static void Unhook(FrameworkElement? editingElement)
	{
		if (editingElement is null)
			return;

		editingElement.Loaded -= EditingElement_Loaded;
		editingElement.LosingFocus -= EditingElement_LosingFocus;
		editingElement.LostFocus -= EditingElement_LostFocus;
		editingElement.KeyDown -= EditingElement_KeyDown;
	}

	public static void Refocus(FrameworkElement editingElement)
	{
		editingElement.DispatcherQueue.TryEnqueue(() => FocusEditingElement(editingElement));
	}

	private static void EditingElement_Loaded(object sender, RoutedEventArgs e)
	{
		if (sender is not FrameworkElement editingElement)
			return;

		editingElement.Loaded -= EditingElement_Loaded;
		editingElement.DispatcherQueue.TryEnqueue(() =>
		{
			if (editingElement.FindAscendant<TableViewCell>() is not { IsEditing: true } cell ||
				cell.EditingElement != editingElement)
			{
				return;
			}

			FocusEditingElement(editingElement);
		});
	}

	private static void EditingElement_KeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (sender is not FrameworkElement editingElement ||
			editingElement.FindAscendant<TableViewCell>() is not { } cell)
		{
			return;
		}

		if (e.Key is VirtualKey.Enter)
		{
			cell.CommitEdit();
			e.Handled = true;
		}
		else if (e.Key is VirtualKey.Escape)
		{
			cell.CancelEdit();
			e.Handled = true;
		}
	}

	private static void EditingElement_LosingFocus(UIElement sender, LosingFocusEventArgs args)
	{
		if (sender is FrameworkElement editingElement && IsProtectedFocusTarget(editingElement, args.NewFocusedElement))
			args.TryCancel();
	}

	private static void EditingElement_LostFocus(object sender, RoutedEventArgs e)
	{
		if (sender is not FrameworkElement editingElement)
			return;

		editingElement.DispatcherQueue.TryEnqueue(() =>
		{
			if (editingElement.FindAscendant<TableViewCell>() is not { IsEditing: true } cell ||
				IsProtectedFocusTarget(
					editingElement,
					editingElement.XamlRoot is null ? null : FocusManager.GetFocusedElement(editingElement.XamlRoot)))
			{
				return;
			}

			cell.CancelEdit(TableViewEditEndingReason.FocusLost);
		});
	}

	private static bool IsProtectedFocusTarget(FrameworkElement editingElement, object? focusedElement)
	{
		if (focusedElement is null || ReferenceEquals(focusedElement, editingElement))
			return true;

		if (focusedElement is FlyoutBase or Popup or FlyoutPresenter or MenuFlyoutPresenter)
			return true;

		return focusedElement is DependencyObject dependencyObject &&
			(IsDescendantOf(dependencyObject, editingElement) ||
				dependencyObject.FindAscendant<Popup>() is not null ||
				dependencyObject.FindAscendant<FlyoutPresenter>() is not null ||
				dependencyObject.FindAscendant<MenuFlyoutPresenter>() is not null);
	}

	private static bool IsDescendantOf(DependencyObject element, DependencyObject ancestor)
	{
		for (var current = element; current is not null; current = VisualTreeHelper.GetParent(current))
		{
			if (ReferenceEquals(current, ancestor))
				return true;
		}

		return false;
	}

	private static void FocusEditingElement(FrameworkElement editingElement)
	{
		var focusTarget = FindFocusTarget(editingElement);
		focusTarget?.Focus(FocusState.Programmatic);
		if (focusTarget is TextBox textBox)
			textBox.SelectAll();
	}

	private static Control? FindFocusTarget(DependencyObject root)
	{
		if (root is Control { IsEnabled: true, IsTabStop: true } control)
			return control;

		var childCount = VisualTreeHelper.GetChildrenCount(root);
		for (var index = 0; index < childCount; index++)
		{
			if (FindFocusTarget(VisualTreeHelper.GetChild(root, index)) is { } child)
				return child;
		}

		return null;
	}
}

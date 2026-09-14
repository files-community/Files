// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using WinRT;

namespace Files.App.Controls;

[TemplateVisualState(Name = HorizontalStateName, GroupName = OrientationStateGroupName)]
[TemplateVisualState(Name = VerticalStateName, GroupName = OrientationStateGroupName)]
public sealed partial class PropertiesViewItemSeparator : Control
{
	private const string OrientationStateGroupName = "OrientationStateGroup";
	private const string HorizontalStateName = "HorizontalState";
	private const string VerticalStateName = "VerticalState";

	public PropertiesViewItemSeparator()
	{
		DefaultStyleKey = typeof(PropertiesViewItemSeparator);
	}

	public Orientation Orientation
	{
		[DynamicWindowsRuntimeCast(typeof(Orientation))]
		get => (Orientation)GetValue(OrientationProperty);
		set => SetValue(OrientationProperty, value);
	}

	public static readonly DependencyProperty OrientationProperty =
		DependencyProperty.Register(
			nameof(Orientation),
			typeof(Orientation),
			typeof(PropertiesViewItemSeparator),
			new PropertyMetadata(Orientation.Horizontal, OnOrientationChanged));

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();
		UpdateOrientationState(Orientation);
	}

	private static void OnOrientationChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
	{
		var separator = (PropertiesViewItemSeparator)dependencyObject;
		separator.UpdateOrientationState((Orientation)eventArgs.NewValue);
	}

	private void UpdateOrientationState(Orientation orientation)
	{
		VisualStateManager.GoToState(
			this,
			orientation is Orientation.Horizontal ? HorizontalStateName : VerticalStateName,
			false);
	}
}

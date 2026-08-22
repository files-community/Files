// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Files.App.Controls;

[TemplatePart(Name = ActionIconPresenterHolderName, Type = typeof(Viewbox))]
//[TemplatePart(Name = HeaderIconPresenterHolderName, Type = typeof(Viewbox))]
[TemplatePart(Name = HeaderPresenterName, Type = typeof(ContentPresenter))]

[TemplateVisualState(Name = NormalStateName, GroupName = CommonStatesName)]
[TemplateVisualState(Name = PointerOverStateName, GroupName = CommonStatesName)]
[TemplateVisualState(Name = PressedStateName, GroupName = CommonStatesName)]
[TemplateVisualState(Name = DisabledStateName, GroupName = CommonStatesName)]

[TemplateVisualState(Name = RightStateName, GroupName = ContentAlignmentStatesName)]
[TemplateVisualState(Name = RightWrappedStateName, GroupName = ContentAlignmentStatesName)]
[TemplateVisualState(Name = LeftStateName, GroupName = ContentAlignmentStatesName)]
[TemplateVisualState(Name = VerticalStateName, GroupName = ContentAlignmentStatesName)]
public partial class PropertiesViewCard : Button
{
	private const string CommonStatesName = "CommonStates";
	private const string NormalStateName = "Normal";
	private const string PointerOverStateName = "PointerOver";
	private const string PressedStateName = "Pressed";
	private const string DisabledStateName = "Disabled";
	private const string ContentAlignmentStatesName = "ContentAlignmentStates";
	private const string RightStateName = "Right";
	private const string RightWrappedStateName = "RightWrapped";
	private const string LeftStateName = "Left";
	private const string VerticalStateName = "Vertical";
	private const string ContentSpacingStateName = "ContentSpacing";
	private const string NoContentSpacingStateName = "NoContentSpacing";
	//private const string BitmapHeaderIconEnabledStateName = "BitmapHeaderIconEnabled";
	//private const string BitmapHeaderIconDisabledStateName = "BitmapHeaderIconDisabled";
	private const string ActionIconPresenterHolderName = "PART_ActionIconPresenterHolder";
	//private const string HeaderIconPresenterHolderName = "PART_HeaderIconPresenterHolder";
	private const string HeaderPresenterName = "PART_HeaderPresenter";

	private VisualStateGroup? contentAlignmentStates;
	private long? contentPropertyChangedToken;

	public PropertiesViewCard()
	{
		DefaultStyleKey = typeof(PropertiesViewCard);
		ActionIcon = new FontIcon { Glyph = "\uE974", MirroredWhenRightToLeft = true };
	}

	protected override void OnApplyTemplate()
	{
		DisableButtonInteraction();
		IsEnabledChanged -= OnIsEnabledChanged;

		if (contentAlignmentStates is not null)
			contentAlignmentStates.CurrentStateChanged -= ContentAlignmentStates_CurrentStateChanged;

		if (contentPropertyChangedToken is long token)
			UnregisterPropertyChangedCallback(ContentProperty, token);

		base.OnApplyTemplate();

		UpdateActionIcon();
		UpdateHeader();
		//UpdateHeaderIcon();
		UpdateClickInteraction();
		UpdateCommonState(false);
		//UpdateBitmapHeaderIconState(false);
		SetAccessibleContentName();

		contentPropertyChangedToken = RegisterPropertyChangedCallback(ContentProperty, OnContentChanged);
		IsEnabledChanged += OnIsEnabledChanged;

		contentAlignmentStates = GetTemplateChild(ContentAlignmentStatesName) as VisualStateGroup;
		if (contentAlignmentStates is not null)
		{
			UpdateContentSpacingState(contentAlignmentStates.CurrentState, false);
			contentAlignmentStates.CurrentStateChanged += ContentAlignmentStates_CurrentStateChanged;
		}
	}

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new PropertiesViewCardAutomationPeer(this);
	}

	protected override void OnPointerPressed(PointerRoutedEventArgs e)
	{
		if (!IsClickEnabled)
			return;

		base.OnPointerPressed(e);
		VisualStateManager.GoToState(this, PressedStateName, true);
	}

	protected override void OnPointerReleased(PointerRoutedEventArgs e)
	{
		if (!IsClickEnabled)
			return;

		base.OnPointerReleased(e);
		VisualStateManager.GoToState(this, NormalStateName, true);
	}

	protected override void OnPointerEntered(PointerRoutedEventArgs e)
	{
		if (!IsClickEnabled)
			return;

		base.OnPointerEntered(e);
		VisualStateManager.GoToState(this, PointerOverStateName, true);
	}

	protected override void OnPointerExited(PointerRoutedEventArgs e)
	{
		if (!IsClickEnabled)
			return;

		base.OnPointerExited(e);
		VisualStateManager.GoToState(this, NormalStateName, true);
	}

	protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
	{
		if (!IsClickEnabled)
			return;

		base.OnPointerCaptureLost(e);
		VisualStateManager.GoToState(this, NormalStateName, true);
	}

	protected override void OnPointerCanceled(PointerRoutedEventArgs e)
	{
		if (!IsClickEnabled)
			return;

		base.OnPointerCanceled(e);
		VisualStateManager.GoToState(this, NormalStateName, true);
	}

	protected override void OnKeyDown(KeyRoutedEventArgs e)
	{
		if (IsClickEnabled)
			base.OnKeyDown(e);
	}

	protected override void OnKeyUp(KeyRoutedEventArgs e)
	{
		if (IsClickEnabled)
			base.OnKeyUp(e);
	}

	partial void OnActionIconChanged(IconElement? newValue)
	{
		UpdateActionIcon();
	}

	partial void OnContentAlignmentChanged(PropertiesViewCardContentAlignment newValue)
	{
		UpdateContentSpacingState(contentAlignmentStates?.CurrentState, true);
	}

	partial void OnHeaderChanged(object? newValue)
	{
		UpdateHeader();
		SetAccessibleContentName();
	}

	//partial void OnHeaderIconChanged(IconElement? newValue)
	//{
	//	UpdateHeaderIcon();
	//	UpdateBitmapHeaderIconState(true);
	//}

	partial void OnIsActionIconVisibleChanged(bool newValue)
	{
		UpdateActionIcon();
	}

	partial void OnIsClickEnabledChanged(bool newValue)
	{
		UpdateClickInteraction();
		UpdateActionIcon();
	}

	private void UpdateClickInteraction()
	{
		if (IsClickEnabled)
			EnableButtonInteraction();
		else
			DisableButtonInteraction();
	}

	private void EnableButtonInteraction()
	{
		DisableButtonInteraction();
		IsTabStop = true;
		PreviewKeyDown += Control_PreviewKeyDown;
		PreviewKeyUp += Control_PreviewKeyUp;
	}

	private void DisableButtonInteraction()
	{
		IsTabStop = false;
		PreviewKeyDown -= Control_PreviewKeyDown;
		PreviewKeyUp -= Control_PreviewKeyUp;

		if (IsEnabled)
			VisualStateManager.GoToState(this, NormalStateName, false);
	}

	private void Control_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (e.Key is VirtualKey.Enter or VirtualKey.Space or VirtualKey.GamepadA &&
			XamlRoot is not null &&
			ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), this))
		{
			VisualStateManager.GoToState(this, PressedStateName, true);
		}
	}

	private void Control_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
	{
		if (e.Key is VirtualKey.Enter or VirtualKey.Space or VirtualKey.GamepadA)
			VisualStateManager.GoToState(this, NormalStateName, true);
	}

	private void OnContentChanged(DependencyObject sender, DependencyProperty property)
	{
		SetAccessibleContentName();
		UpdateContentSpacingState(contentAlignmentStates?.CurrentState, false);
	}

	private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		UpdateCommonState(true);
		//UpdateBitmapHeaderIconState(true);
	}

	private void UpdateCommonState(bool useTransitions)
	{
		VisualStateManager.GoToState(this, IsEnabled ? NormalStateName : DisabledStateName, useTransitions);
	}

	private void UpdateActionIcon()
	{
		if (GetTemplateChild(ActionIconPresenterHolderName) is FrameworkElement presenter)
			presenter.Visibility = IsClickEnabled && IsActionIconVisible && ActionIcon is not null
				? Visibility.Visible
				: Visibility.Collapsed;
	}

	private void UpdateHeader()
	{
		if (GetTemplateChild(HeaderPresenterName) is FrameworkElement presenter)
			presenter.Visibility = IsNullOrEmptyString(Header) ? Visibility.Collapsed : Visibility.Visible;
	}

	//private void UpdateHeaderIcon()
	//{
	//	if (GetTemplateChild(HeaderIconPresenterHolderName) is FrameworkElement presenter)
	//		presenter.Visibility = HeaderIcon is null ? Visibility.Collapsed : Visibility.Visible;
	//}

	//private void UpdateBitmapHeaderIconState(bool useTransitions)
	//{
	//	VisualStateManager.GoToState(
	//		this,
	//		HeaderIcon is BitmapIcon && !IsEnabled
	//			? BitmapHeaderIconDisabledStateName
	//			: BitmapHeaderIconEnabledStateName,
	//		useTransitions);
	//}

	private void ContentAlignmentStates_CurrentStateChanged(object sender, VisualStateChangedEventArgs e)
	{
		UpdateContentSpacingState(e.NewState, true);
	}

	private void UpdateContentSpacingState(VisualState? state, bool useTransitions)
	{
		bool isVertical = state?.Name is RightWrappedStateName or VerticalStateName;
		bool hasHeader = !IsNullOrEmptyString(Header);
		VisualStateManager.GoToState(
			this,
			isVertical && Content is not null && hasHeader ? ContentSpacingStateName : NoContentSpacingStateName,
			useTransitions);
	}

	private void SetAccessibleContentName()
	{
		if (Header is not string { Length: > 0 } header ||
			Content is not UIElement element ||
			element is ButtonBase or TextBlock ||
			!string.IsNullOrEmpty(AutomationProperties.GetName(element)))
		{
			return;
		}

		AutomationProperties.SetName(element, header);
	}

	private static bool IsNullOrEmptyString(object? value)
	{
		return value is null or "";
	}
}

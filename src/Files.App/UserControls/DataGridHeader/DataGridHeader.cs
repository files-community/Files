// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls
{
	/// <summary>
	/// Represents button control called <see cref="DataGridHeader"/>.
	/// Most likely to be used in columns view layout page.
	/// </summary>
	public class DataGridHeader : ButtonBase
	{
		internal const string NormalState = "Normal";
		internal const string PointerOverState = "PointerOver";
		internal const string PressedState = "Pressed";
		internal const string DisabledState = "Disabled";

		internal const string UnsortedState = "Unsorted";
		internal const string SortAscendingState = "SortAscending";
		internal const string SortDescendingState = "SortDescending";

		internal const string HeaderContentPresenter = "HeaderContentPresenter";

		internal static bool IsXamlRootAvailable = Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent("Windows.UI.Xaml.UIElement", "XamlRoot");

		public static readonly DependencyProperty HeaderProperty =
			DependencyProperty.Register(
				nameof(Header),
				typeof(string),
				typeof(DataGridHeader),
				new PropertyMetadata(null, (d, e) => ((DataGridHeader)d).OnHeaderPropertyChanged()));

		public string Header
		{
			get => (string)GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public static readonly DependencyProperty ColumnSortOptionProperty =
			DependencyProperty.Register(
				nameof(ColumnSortOption),
				typeof(SortDirection?),
				typeof(DataGridHeader),
				new PropertyMetadata(null, (d, e) => ((DataGridHeader)d).OnColumnSortOptionPropertyChanged()));

		public SortDirection? ColumnSortOption
		{
			get => (SortDirection?)GetValue(ColumnSortOptionProperty);
			set => SetValue(ColumnSortOptionProperty, value);
		}

		public DataGridHeader()
		{
			EnableButtonInteraction();
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			IsEnabledChanged -= OnIsEnabledChanged;
			OnHeaderPropertyChanged();
			VisualStateManager.GoToState(this, IsEnabled ? NormalState : DisabledState, true);
			RegisterAutomation();
			IsEnabledChanged += OnIsEnabledChanged;
		}

		private void RegisterAutomation()
		{
			if (Header is string headerString && headerString != string.Empty)
			{
				AutomationProperties.SetName(this, headerString);

				// We don't want to override an AutomationProperties.Name that is manually set, or if the Content basetype is of type ButtonBase (the ButtonBase.Content will be used then)
				if (Content is UIElement element && string.IsNullOrEmpty(AutomationProperties.GetName(element)) && element.GetType().BaseType != typeof(ButtonBase) && element.GetType() != typeof(TextBlock))
					AutomationProperties.SetName(element, headerString);
			}
		}

		private void EnableButtonInteraction()
		{
			DisableButtonInteraction();

			IsTabStop = true;
			PointerEntered += Control_PointerEntered;
			PointerExited += Control_PointerExited;
			PointerCaptureLost += Control_PointerCaptureLost;
			PointerCanceled += Control_PointerCanceled;
			PreviewKeyDown += Control_PreviewKeyDown;
			PreviewKeyUp += Control_PreviewKeyUp;
		}

		private void DisableButtonInteraction()
		{
			IsTabStop = false;
			PointerEntered -= Control_PointerEntered;
			PointerExited -= Control_PointerExited;
			PointerCaptureLost -= Control_PointerCaptureLost;
			PointerCanceled -= Control_PointerCanceled;
			PreviewKeyDown -= Control_PreviewKeyDown;
			PreviewKeyUp -= Control_PreviewKeyUp;
		}

		private void Control_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == Windows.System.VirtualKey.Enter || e.Key == Windows.System.VirtualKey.Space || e.Key == Windows.System.VirtualKey.GamepadA)
			{
				VisualStateManager.GoToState(this, NormalState, true);
			}
		}

		private void Control_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == Windows.System.VirtualKey.Enter || e.Key == Windows.System.VirtualKey.Space || e.Key == Windows.System.VirtualKey.GamepadA)
			{
				// Check if the active focus is on the card itself - only then we show the pressed state.
				if (GetFocusedElement() is DataGridHeader)
					VisualStateManager.GoToState(this, PressedState, true);
			}
		}

		public void Control_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			base.OnPointerEntered(e);
			VisualStateManager.GoToState(this, PointerOverState, true);
		}

		public void Control_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			base.OnPointerExited(e);
			VisualStateManager.GoToState(this, NormalState, true);
		}

		private void Control_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
		{
			base.OnPointerCaptureLost(e);
			VisualStateManager.GoToState(this, NormalState, true);
		}

		private void Control_PointerCanceled(object sender, PointerRoutedEventArgs e)
		{
			base.OnPointerCanceled(e);
			VisualStateManager.GoToState(this, NormalState, true);
		}

		protected override void OnPointerPressed(PointerRoutedEventArgs e)
		{
			//  e.Handled = true;
			base.OnPointerPressed(e);
			VisualStateManager.GoToState(this, PressedState, true);
		}

		protected override void OnPointerReleased(PointerRoutedEventArgs e)
		{
			base.OnPointerReleased(e);
			VisualStateManager.GoToState(this, NormalState, true);
		}

		// TODO: AutomationPeer probably need to be implemented
		//protected override AutomationPeer OnCreateAutomationPeer()
		//{
		//	return new DataGridHeaderAutomationPeer(this);
		//}

		private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			VisualStateManager.GoToState(this, IsEnabled ? NormalState : DisabledState, true);
		}

		private void OnHeaderPropertyChanged()
		{
			if (GetTemplateChild(HeaderContentPresenter) is FrameworkElement headerContentPresenter)
			{
				headerContentPresenter.Visibility = Header != null
					? Visibility.Visible
					: Visibility.Collapsed;
			}
		}

		private void OnColumnSortOptionPropertyChanged()
		{
			_ = ColumnSortOption switch
			{
				SortDirection.Ascending => VisualStateManager.GoToState(this, SortAscendingState, true),
				SortDirection.Descending => VisualStateManager.GoToState(this, SortDescendingState, true),
				_ => VisualStateManager.GoToState(this, UnsortedState, true),
			};
		}

		private FrameworkElement? GetFocusedElement()
		{
			if (IsXamlRootAvailable && XamlRoot != null)
				return FocusManager.GetFocusedElement(XamlRoot) as FrameworkElement;
			else
				return FocusManager.GetFocusedElement() as FrameworkElement;
		}
	}
}

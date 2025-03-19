// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI;
using Windows.ApplicationModel.Contacts;

namespace Files.App.Controls
{
	// Content
	[ContentProperty(Name = nameof(Modes))]
	// Template parts
	[TemplatePart(Name = "PART_ModesHostGrid", Type = typeof(Grid))]
	// Visual states
	[TemplateVisualState(Name = "Focused", GroupName = "FocusStates")]
	[TemplateVisualState(Name = "Normal", GroupName = "FocusStates")]
	public partial class Omnibar : Control
	{
		private const string ModesHostGrid = "PART_ModesHostGrid";
		private const string AutoSuggestPopup = "PART_AutoSuggestPopup";
		private const string AutoSuggestBoxBorder = "PART_AutoSuggestBoxBorder";

		private Grid? _modesHostGrid;
		private Popup? _autoSuggestPopup;
		private Border? _autoSuggestBoxBorder;
		private bool _isFocused;
		private bool _stillHasFocus;

		public Omnibar()
		{
			DefaultStyleKey = typeof(Omnibar);

			Modes ??= [];
		}

		protected override void OnApplyTemplate()
		{
			_modesHostGrid = GetTemplateChild(ModesHostGrid) as Grid
				?? throw new MissingFieldException($"Could not find {ModesHostGrid} in the given {nameof(Omnibar)}'s style.");
			_autoSuggestPopup = GetTemplateChild(AutoSuggestPopup) as Popup
				?? throw new MissingFieldException($"Could not find {AutoSuggestPopup} in the given {nameof(Omnibar)}'s style.");
			_autoSuggestBoxBorder = GetTemplateChild(AutoSuggestBoxBorder) as Border
				?? throw new MissingFieldException($"Could not find {AutoSuggestBoxBorder} in the given {nameof(Omnibar)}'s style.");

			if (Modes is null)
				return;

			// Add shadow to the popup and set the proper width
			_autoSuggestBoxBorder!.Translation = new(0, 0, 32);
			_autoSuggestBoxBorder!.Width = _modesHostGrid!.ActualWidth;

			// Populate the modes
			foreach (var mode in Modes)
			{
				// Insert a divider
				if (_modesHostGrid.Children.Count is not 0)
				{
					var divider = new Rectangle()
					{
						Fill = (SolidColorBrush)Application.Current.Resources["DividerStrokeColorDefaultBrush"],
						Height = 20,
						Margin = new(2,0,2,0),
						Width = 1,
					};

					_modesHostGrid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
					Grid.SetColumn(divider, _modesHostGrid.Children.Count);
					_modesHostGrid.Children.Add(divider);
				}

				// Insert the mode
				_modesHostGrid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
				Grid.SetColumn(mode, _modesHostGrid.Children.Count);
				_modesHostGrid.Children.Add(mode);
				mode.Host = this;
			}

			_modesHostGrid.SizeChanged += ModesHostGrid_SizeChanged;

			GotFocus += Omnibar_GotFocus;
			LostFocus += Omnibar_LostFocus;
			LosingFocus += Omnibar_LosingFocus;

			UpdateVisualStates();

			base.OnApplyTemplate();
		}

		// Methods

		internal void ChangeMode(OmnibarMode modeToExpand)
		{
			if (_modesHostGrid is null || Modes is null)
				throw new NullReferenceException();

			// Reset
			foreach (var column in _modesHostGrid.ColumnDefinitions)
				column.Width = GridLength.Auto;
			foreach (var mode in Modes)
				VisualStateManager.GoToState(mode, "Unfocused", true);

			// Expand the given mode
			VisualStateManager.GoToState(modeToExpand, "Focused", true);
			_modesHostGrid.ColumnDefinitions[_modesHostGrid.Children.IndexOf(modeToExpand)].Width = new(1, GridUnitType.Star);

			CurrentSelectedMode = modeToExpand;

			UpdateVisualStates();
		}

		private void UpdateVisualStates()
		{
			VisualStateManager.GoToState(this, _isFocused ? "Focused" : "Normal", true);

			if (CurrentSelectedMode is not null && _autoSuggestPopup is not null)
			{
				// Close anyway
				if (_autoSuggestPopup.IsOpen && CurrentSelectedMode.SuggestionItemsSource is null)
					VisualStateManager.GoToState(this, "PopupClosed", true);

				// Decide open or close
				if (_isFocused != _autoSuggestPopup.IsOpen)
					VisualStateManager.GoToState(this, _isFocused && CurrentSelectedMode.SuggestionItemsSource is not null ? "PopupOpened" : "PopupClosed", true);
			}

			if (CurrentSelectedMode is not null)
				VisualStateManager.GoToState(
					CurrentSelectedMode,
					_isFocused
						? "Focused"
						: CurrentSelectedMode.ContentOnInactive is null
							? "CurrentUnfocusedWithoutInactiveMode"
							: "CurrentUnfocusedWithInactiveMode",
					true);
		}

		// Events

		private void ModesHostGrid_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			_autoSuggestBoxBorder!.Width = _modesHostGrid!.ActualWidth;
		}

		private void Omnibar_GotFocus(object sender, RoutedEventArgs e)
		{
			_isFocused = true;
			UpdateVisualStates();
		}

		private void Omnibar_LosingFocus(UIElement sender, LosingFocusEventArgs args)
		{
			// Ignore when user clicks on the TextBox or the button area of an OmnibarMode, Omnibar still has focus anyway
			if (args.NewFocusedElement?.GetType() is not { } focusedType ||
				focusedType == typeof(TextBox) ||
				focusedType == typeof(OmnibarMode) ||
				focusedType == typeof(Omnibar))
			{
				_stillHasFocus = true;
			}
		}

		private void Omnibar_LostFocus(object sender, RoutedEventArgs e)
		{
			if (_stillHasFocus)
			{
				_stillHasFocus = false;
				return;
			}

			_isFocused = false;
			UpdateVisualStates();
		}
	}
}

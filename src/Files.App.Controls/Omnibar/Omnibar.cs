// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;

namespace Files.App.Controls
{
	// Content
	[ContentProperty(Name = nameof(Modes))]
	public partial class Omnibar : Control
	{
		// Constants

		private const string TemplatePartName_AutoSuggestBox = "PART_TextBox";
		private const string TemplatePartName_ModesHostGrid = "PART_ModesHostGrid";
		private const string TemplatePartName_AutoSuggestBoxSuggestionsPopup = "PART_SuggestionsPopup";
		private const string TemplatePartName_AutoSuggestBoxSuggestionsContainerBorder = "PART_SuggestionsContainerBorder";
		private const string TemplatePartName_SuggestionsListView = "PART_SuggestionsListView";

		// Fields

		private TextBox _textBox = null!;
		private Grid _modesHostGrid = null!;
		private Popup _textBoxSuggestionsPopup = null!;
		private Border _textBoxSuggestionsContainerBorder = null!;
		private ListView _textBoxSuggestionsListView = null!;

		private bool _isFocused;
		private string _userInput = string.Empty;
		private OmnibarTextChangeReason _textChangeReason = OmnibarTextChangeReason.None;

		// Events

		public event TypedEventHandler<Omnibar, OmnibarQuerySubmittedEventArgs>? QuerySubmitted;
		public event TypedEventHandler<Omnibar, OmnibarSuggestionChosenEventArgs>? SuggestionChosen;
		public event TypedEventHandler<Omnibar, OmnibarTextChangedEventArgs>? TextChanged;

		// Constructor

		public Omnibar()
		{
			DefaultStyleKey = typeof(Omnibar);

			Modes = [];
			AutoSuggestBoxPadding = new(0, 0, 0, 0);
		}

		// Methods

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_textBox = GetTemplateChild(TemplatePartName_AutoSuggestBox) as TextBox
				?? throw new MissingFieldException($"Could not find {TemplatePartName_AutoSuggestBox} in the given {nameof(Omnibar)}'s style.");
			_modesHostGrid = GetTemplateChild(TemplatePartName_ModesHostGrid) as Grid
				?? throw new MissingFieldException($"Could not find {TemplatePartName_ModesHostGrid} in the given {nameof(Omnibar)}'s style.");
			_textBoxSuggestionsPopup = GetTemplateChild(TemplatePartName_AutoSuggestBoxSuggestionsPopup) as Popup
				?? throw new MissingFieldException($"Could not find {TemplatePartName_AutoSuggestBoxSuggestionsPopup} in the given {nameof(Omnibar)}'s style.");
			_textBoxSuggestionsContainerBorder = GetTemplateChild(TemplatePartName_AutoSuggestBoxSuggestionsContainerBorder) as Border
				?? throw new MissingFieldException($"Could not find {TemplatePartName_AutoSuggestBoxSuggestionsContainerBorder} in the given {nameof(Omnibar)}'s style.");
			_textBoxSuggestionsListView = GetTemplateChild(TemplatePartName_SuggestionsListView) as ListView
				?? throw new MissingFieldException($"Could not find {TemplatePartName_SuggestionsListView} in the given {nameof(Omnibar)}'s style.");

			PopulateModes();

			SizeChanged += Omnibar_SizeChanged;
			_textBox.GotFocus += AutoSuggestBox_GotFocus;
			_textBox.LostFocus += AutoSuggestBox_LostFocus;
			_textBox.KeyDown += AutoSuggestBox_KeyDown;
			_textBox.TextChanged += AutoSuggestBox_TextChanged;
			_textBoxSuggestionsPopup.GettingFocus += AutoSuggestBoxSuggestionsPopup_GettingFocus;
			_textBoxSuggestionsListView.ItemClick += AutoSuggestBoxSuggestionsListView_ItemClick;

			// Set the default width
			_textBoxSuggestionsContainerBorder.Width = ActualWidth;
		}

		public void PopulateModes()
		{
			if (Modes is null || _modesHostGrid is null)
				return;

			// Populate the modes
			foreach (var mode in Modes)
			{
				// Insert a divider
				if (_modesHostGrid.Children.Count is not 0)
				{
					var divider = new OmnibarModeSeparator();

					_modesHostGrid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
					Grid.SetColumn(divider, _modesHostGrid.Children.Count);
					_modesHostGrid.Children.Add(divider);
				}

				// Insert the mode
				_modesHostGrid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
				Grid.SetColumn(mode, _modesHostGrid.Children.Count);
				_modesHostGrid.Children.Add(mode);
				mode.SetOwner(this);
			}
		}

		public void ChangeMode(OmnibarMode modeToExpand, bool shouldFocus = false, bool useTransition = true)
		{
			if (_modesHostGrid is null || Modes is null)
				return;

			foreach (var mode in Modes)
			{
				// Add the reposition transition to the all modes
				if (useTransition)
				{
					mode.Transitions = [new RepositionThemeTransition()];
					mode.UpdateLayout();
				}

				mode.OnChangingCurrentMode(false);
			}

			var index = _modesHostGrid.Children.IndexOf(modeToExpand);

			if (CurrentSelectedMode is not null)
				VisualStateManager.GoToState(CurrentSelectedMode, "Unfocused", true);

			// Reset
			foreach (var column in _modesHostGrid.ColumnDefinitions)
				column.Width = GridLength.Auto;

			// Expand the given mode
			_modesHostGrid.ColumnDefinitions[index].Width = new(1, GridUnitType.Star);

			var itemCount = Modes.Count;
			var itemIndex = Modes.IndexOf(modeToExpand);
			var modeButtonWidth = modeToExpand.ActualWidth;
			var modeSeparatorWidth = itemCount is not 0 or 1 ? _modesHostGrid.Children[1] is FrameworkElement frameworkElement ? frameworkElement.ActualWidth : 0 : 0;

			var leftPadding = (itemIndex + 1) * modeButtonWidth + modeSeparatorWidth * itemIndex;
			var rightPadding = (itemCount - itemIndex - 1) * modeButtonWidth + modeSeparatorWidth * (itemCount - itemIndex - 1) + 8;

			// Set the correct AutoSuggestBox cursor position
			AutoSuggestBoxPadding = new(leftPadding, 0, rightPadding, 0);

			CurrentSelectedMode = modeToExpand;

			_textChangeReason = OmnibarTextChangeReason.ProgrammaticChange;
			_textBox.Text = CurrentSelectedMode.Text ?? string.Empty;

			// Move cursor of the TextBox to the tail
			_textBox.Select(_textBox.Text.Length, 0);

			VisualStateManager.GoToState(CurrentSelectedMode, "Focused", true);
			CurrentSelectedMode.OnChangingCurrentMode(true);

			if (_isFocused)
			{
				VisualStateManager.GoToState(CurrentSelectedMode, "Focused", true);
				VisualStateManager.GoToState(_textBox, "InputAreaVisible", true);
			}
			else if (CurrentSelectedMode?.ContentOnInactive is not null)
			{
				VisualStateManager.GoToState(CurrentSelectedMode, "CurrentUnfocused", true);
				VisualStateManager.GoToState(_textBox, "InputAreaCollapsed", true);
			}
			else
			{
				VisualStateManager.GoToState(_textBox, "InputAreaVisible", true);
			}

			if (shouldFocus)
				_textBox.Focus(FocusState.Keyboard);

			TryToggleIsSuggestionsPopupOpen(_isFocused && CurrentSelectedMode?.SuggestionItemsSource is not null);

			// Remove the reposition transition from the all modes
			if (useTransition)
			{
				foreach (var mode in Modes)
				{
					mode.Transitions.Clear();
					mode.UpdateLayout();
				}
			}
		}

		public bool TryToggleIsSuggestionsPopupOpen(bool wantToOpen)
		{
			if (wantToOpen && (!_isFocused || CurrentSelectedMode?.SuggestionItemsSource is null))
				return false;

			_textBoxSuggestionsPopup.IsOpen = wantToOpen;

			return false;
		}

		public void ChooseSuggestionItem(object obj)
		{
			if (CurrentSelectedMode is null)
				return;

			if (CurrentSelectedMode.UpdateTextOnSelect)
			{
				_textChangeReason = OmnibarTextChangeReason.SuggestionChosen;
				_textBox.Text = GetObjectText(obj);
			}

			SuggestionChosen?.Invoke(this, new(CurrentSelectedMode, obj));

			// Move the cursor to the end of the TextBox
			_textBox?.Select(_textBox.Text.Length, 0);
		}

		private void SubmitQuery(object? item)
		{
			if (CurrentSelectedMode is null)
				return;

			QuerySubmitted?.Invoke(this, new OmnibarQuerySubmittedEventArgs(CurrentSelectedMode, item, _textBox.Text));

			_textBoxSuggestionsPopup.IsOpen = false;
		}

		private string GetObjectText(object obj)
		{
			if (CurrentSelectedMode is null)
				return string.Empty;

			// Get the text to put into the text box from the chosen suggestion item
			return obj is string text
				? text
				: obj is IOmnibarTextMemberPathProvider textMemberPathProvider
					? textMemberPathProvider.GetTextMemberPath(CurrentSelectedMode.DisplayMemberPath ?? string.Empty)
					: obj.ToString() ?? string.Empty;
		}

		private void RevertTextToUserInput()
		{
			if (CurrentSelectedMode is null)
				return;

			_textBoxSuggestionsListView.SelectedIndex = -1;
			_textChangeReason = OmnibarTextChangeReason.ProgrammaticChange;

			_textBox.Text = _userInput ?? "";

			// Move the cursor to the end of the TextBox
			_textBox?.Select(_textBox.Text.Length, 0);
		}
	}
}

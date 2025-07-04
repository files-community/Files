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

		private string _userInput = string.Empty;
		private OmnibarTextChangeReason _textChangeReason = OmnibarTextChangeReason.None;

		private WeakReference<UIElement?> _previouslyFocusedElement = new(null);

		// NOTE: This is a workaround to keep Omnibar's focus on a mode button being clicked
		internal bool IsModeButtonPressed { get; set; }

		// Events

		public event TypedEventHandler<Omnibar, OmnibarQuerySubmittedEventArgs>? QuerySubmitted;
		public event TypedEventHandler<Omnibar, OmnibarSuggestionChosenEventArgs>? SuggestionChosen;
		public event TypedEventHandler<Omnibar, OmnibarTextChangedEventArgs>? TextChanged;
		public event TypedEventHandler<Omnibar, OmnibarModeChangedEventArgs>? ModeChanged;
		public event TypedEventHandler<Omnibar, OmnibarIsFocusedChangedEventArgs> IsFocusedChanged;

		// Constructor

		public Omnibar()
		{
			DefaultStyleKey = typeof(Omnibar);

			Modes = [];
			AutoSuggestBoxPadding = new(0, 0, 0, 0);

			GlobalHelper.WriteDebugStringForOmnibar("Omnibar has been initialized.");
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
			_textBox.GettingFocus += AutoSuggestBox_GettingFocus;
			_textBox.GotFocus += AutoSuggestBox_GotFocus;
			_textBox.LosingFocus += AutoSuggestBox_LosingFocus;
			_textBox.LostFocus += AutoSuggestBox_LostFocus;
			_textBox.KeyDown += AutoSuggestBox_KeyDown;
			_textBox.TextChanged += AutoSuggestBox_TextChanged;
			_textBoxSuggestionsPopup.GettingFocus += AutoSuggestBoxSuggestionsPopup_GettingFocus;
			_textBoxSuggestionsPopup.Opened += AutoSuggestBoxSuggestionsPopup_Opened;
			_textBoxSuggestionsListView.ItemClick += AutoSuggestBoxSuggestionsListView_ItemClick;
			_textBoxSuggestionsListView.SelectionChanged += AutoSuggestBoxSuggestionsListView_SelectionChanged;

			// Set the default width
			_textBoxSuggestionsContainerBorder.Width = ActualWidth;

			GlobalHelper.WriteDebugStringForOmnibar("The template and the events have been initialized.");
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

		protected void ChangeMode(OmnibarMode? oldMode, OmnibarMode newMode)
		{
			if (_modesHostGrid is null || Modes is null || CurrentSelectedMode is null)
				return;

			foreach (var mode in Modes)
			{
				// Add the reposition transition to the all modes
				mode.Transitions = [new RepositionThemeTransition()];
				mode.UpdateLayout();
				mode.IsTabStop = false;
			}

			var index = _modesHostGrid.Children.IndexOf(newMode);

			if (oldMode is not null)
				VisualStateManager.GoToState(oldMode, "Unfocused", true);

			DispatcherQueue.TryEnqueue(() =>
			{
				// Reset
				foreach (var column in _modesHostGrid.ColumnDefinitions)
					column.Width = GridLength.Auto;

				// Expand the given mode
				_modesHostGrid.ColumnDefinitions[index].Width = new(1, GridUnitType.Star);
			});

			var itemCount = Modes.Count;
			var itemIndex = Modes.IndexOf(newMode);
			var modeButtonWidth = newMode.ActualWidth;
			var modeSeparatorWidth = itemCount is not 0 or 1 ? _modesHostGrid.Children[1] is FrameworkElement frameworkElement ? frameworkElement.ActualWidth : 0 : 0;

			var leftPadding = (itemIndex + 1) * modeButtonWidth + modeSeparatorWidth * itemIndex;
			var rightPadding = (itemCount - itemIndex - 1) * modeButtonWidth + modeSeparatorWidth * (itemCount - itemIndex - 1) + 8;

			// Set the correct AutoSuggestBox cursor position
			AutoSuggestBoxPadding = new(leftPadding, 0, rightPadding, 0);

			_textChangeReason = OmnibarTextChangeReason.ProgrammaticChange;
			ChangeTextBoxText(newMode.Text ?? string.Empty);

			VisualStateManager.GoToState(newMode, "Focused", true);
			newMode.IsTabStop = false;

			ModeChanged?.Invoke(this, new(oldMode, newMode!));

			_textBox.PlaceholderText = newMode.PlaceholderText ?? string.Empty;
			_textBoxSuggestionsListView.ItemTemplate = newMode.ItemTemplate;
			_textBoxSuggestionsListView.ItemsSource = newMode.ItemsSource;

			if (newMode.IsAutoFocusEnabled)
			{
				_textBox.Focus(FocusState.Pointer);
			}
			else
			{
				if (IsFocused)
				{
					VisualStateManager.GoToState(newMode, "Focused", true);
					VisualStateManager.GoToState(_textBox, "InputAreaVisible", true);
				}
				else if (newMode?.ContentOnInactive is not null)
				{
					VisualStateManager.GoToState(newMode, "CurrentUnfocused", true);
					VisualStateManager.GoToState(_textBox, "InputAreaCollapsed", true);
				}
				else
				{
					VisualStateManager.GoToState(_textBox, "InputAreaVisible", true);
				}
			}

			TryToggleIsSuggestionsPopupOpen(true);

			// Remove the reposition transition from the all modes
			foreach (var mode in Modes)
			{
				mode.Transitions.Clear();
				mode.UpdateLayout();
			}

			GlobalHelper.WriteDebugStringForOmnibar($"Successfully changed Mode from {oldMode} to {newMode}");
		}

		internal protected void FocusTextBox()
		{
			_textBox.Focus(FocusState.Keyboard);
		}

		internal protected bool TryToggleIsSuggestionsPopupOpen(bool wantToOpen)
		{
			if (_textBoxSuggestionsPopup is null)
				return false;

			if (wantToOpen && (!IsFocused || CurrentSelectedMode?.ItemsSource is null || (CurrentSelectedMode?.ItemsSource is IList collection && collection.Count is 0)))
			{
				_textBoxSuggestionsPopup.IsOpen = false;

				GlobalHelper.WriteDebugStringForOmnibar("The suggestions pop-up closed.");

				return false;
			}

			_textBoxSuggestionsPopup.IsOpen = wantToOpen;

			GlobalHelper.WriteDebugStringForOmnibar("The suggestions pop-up is open.");

			return false;
		}

		public void ChooseSuggestionItem(object obj, bool isOriginatedFromArrowKey = false)
		{
			if (CurrentSelectedMode is null)
				return;

			if (CurrentSelectedMode.UpdateTextOnSelect ||
				(isOriginatedFromArrowKey && CurrentSelectedMode.UpdateTextOnArrowKeys))
			{
				_textChangeReason = OmnibarTextChangeReason.SuggestionChosen;
				ChangeTextBoxText(GetObjectText(obj));
			}

			SuggestionChosen?.Invoke(this, new(CurrentSelectedMode, obj));
		}

		internal protected void ChangeTextBoxText(string text)
		{
			_textBox.Text = text;

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
					? textMemberPathProvider.GetTextMemberPath(CurrentSelectedMode.TextMemberPath ?? string.Empty)
					: obj.ToString() ?? string.Empty;
		}

		private void RevertTextToUserInput()
		{
			if (CurrentSelectedMode is null)
				return;

			_textBoxSuggestionsListView.SelectedIndex = -1;
			_textChangeReason = OmnibarTextChangeReason.ProgrammaticChange;

			ChangeTextBoxText(_userInput ?? "");
		}
	}
}

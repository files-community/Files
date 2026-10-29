// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.Dialogs;
using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using WinRT;

namespace Files.App.Views.Settings
{
	/// <summary>
	/// Edits a layout template, opened from <see cref="LayoutPage"/>.
	/// </summary>
	public sealed partial class LayoutTemplatePage : Page
	{
		public LayoutTemplateEditorViewModel ViewModel { get; private set; } = null!;

		public LayoutTemplatePage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			if (e.Parameter is LayoutTemplateEditorViewModel viewModel)
			{
				ViewModel = viewModel;
				Bindings.Update();
			}
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			base.OnNavigatedFrom(e);
			ViewModel?.Save();
		}

		private void LayoutBreadcrumb_Click(Hyperlink sender, HyperlinkClickEventArgs args)
		{
			GoBack();
		}

		private void GoBack()
		{
			if (Frame.CanGoBack)
				Frame.GoBack(new SuppressNavigationTransitionInfo());
		}

		private async void Rename_Click(object sender, RoutedEventArgs e)
		{
			if (await LayoutTemplateDialogs.AskNameAsync(Strings.RenameTemplate.GetLocalizedResource(), Strings.Rename.GetLocalizedResource(), ViewModel.DisplayName) is { } name)
				ViewModel.Name = name;
		}

		private void UseForFileTypes_Toggled(object sender, RoutedEventArgs e)
		{
			if (sender is ToggleSwitch toggleSwitch)
				ViewModel.SetUsedForFileTypes(toggleSwitch.IsOn);
		}

		private async void DeleteCard_Click(object sender, RoutedEventArgs e)
		{
			if (!await LayoutTemplateDialogs.ConfirmDeleteAsync(ViewModel.DisplayName))
				return;

			ViewModel.Delete();
			GoBack();
		}

		private void ManageColumns_Click(object sender, RoutedEventArgs e)
		{
			if (ViewModel.CreateColumnsViewModel() is { } columns)
				ColumnCustomizationWindow.Show(columns);
		}

		private void PropertyPicker_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			if (args.Reason is not AutoSuggestionBoxTextChangeReason.UserInput || sender.Tag is not LayoutPropertyPickerViewModel picker)
				return;

			picker.Filter(sender.Text);
			sender.IsSuggestionListOpen = true;
		}

		private void PropertyPicker_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
		{
			if (sender.Tag is not LayoutPropertyPickerViewModel picker)
				return;

			if (args.ChosenSuggestion is null && args.QueryText == picker.Text)
			{
				picker.Filter(null);
				sender.IsSuggestionListOpen = !sender.IsSuggestionListOpen;
				return;
			}

			var choice = args.ChosenSuggestion as LayoutPropertyChoice
				?? picker.Suggestions.FirstOrDefault(choice => choice.DisplayName.Equals(args.QueryText.Trim(), StringComparison.CurrentCultureIgnoreCase))
				?? picker.Suggestions.FirstOrDefault();
			if (choice is not null)
				picker.Choose(choice);

			sender.Text = picker.Text;
			sender.IsSuggestionListOpen = false;
		}

		[DynamicWindowsRuntimeCast(typeof(AutoSuggestBox))]
		private void PropertyPicker_LostFocus(object sender, RoutedEventArgs e)
		{
			if (sender is AutoSuggestBox { Tag: LayoutPropertyPickerViewModel picker, IsSuggestionListOpen: false } autoSuggestBox)
				autoSuggestBox.Text = picker.Text;
		}
	}
}
